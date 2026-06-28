using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using SquidStd.Crypto.Vfs.Data;
using SquidStd.Crypto.Vfs.Internal;
using SquidStd.Vfs.Abstractions.Data;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Abstractions;

namespace SquidStd.Crypto.Vfs.Services;

/// <summary>An encrypted, lockable virtual filesystem decorating any inner <see cref="IVirtualFileSystem" />.</summary>
public sealed class CryptoFileSystem : ILockableFileSystem, IDisposable
{
    private const string HeaderPath = "_vfs_header";
    private const string IndexPath = "_vfs_index";
    private readonly IVirtualFileSystem _inner;
    private readonly CryptoVaultOptions _options;
    private byte[]? _masterKey;
    private VaultIndex? _index;

    public bool IsUnlocked => _masterKey is not null;

    public CryptoFileSystem(IVirtualFileSystem inner, CryptoVaultOptions? options = null)
    {
        _inner = inner;
        _options = options ?? new CryptoVaultOptions();
    }

    public void Unlock(string passphrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

        var headerBytes = _inner.ReadAllBytesAsync(HeaderPath).AsTask().GetAwaiter().GetResult();
        VaultHeader header;

        if (headerBytes is null)
        {
            header = new VaultHeader(
                "SQVFS1",
                1,
                RandomNumberGenerator.GetBytes(16),
                _options.Argon2MemoryKib,
                _options.Argon2Iterations,
                _options.Argon2Parallelism,
                _options.ChunkSize
            );
            _inner.WriteAllBytesAsync(HeaderPath, header.Serialize()).AsTask().GetAwaiter().GetResult();
        }
        else
        {
            header = VaultHeader.Parse(headerBytes);
        }

        var headerOptions = new CryptoVaultOptions
        {
            ChunkSize = header.ChunkSize,
            Argon2MemoryKib = header.MemoryKib,
            Argon2Iterations = header.Iterations,
            Argon2Parallelism = header.Parallelism
        };
        var master = VaultKeyDerivation.DeriveMasterKey(passphrase, header.Salt, headerOptions);

        var indexBytes = _inner.ReadAllBytesAsync(IndexPath).AsTask().GetAwaiter().GetResult();
        _index = indexBytes is null
            ? new VaultIndex()
            : VaultIndex.Parse(VaultBlob.Decrypt(VaultKeyDerivation.DeriveSubKey(master, "index"), indexBytes));

        _masterKey = master;
    }

    public void Lock()
    {
        if (_masterKey is null)
        {
            return;
        }

        FlushIndex();
        PruneOrphans();
        CryptographicOperations.ZeroMemory(_masterKey);
        _masterKey = null;
        _index = null;
    }

    public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureUnlocked();

        return ValueTask.FromResult(_index!.TryGet(VfsPath.Normalize(path), out _));
    }

    public async ValueTask<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureUnlocked();

        if (!_index!.TryGet(VfsPath.Normalize(path), out var entry))
        {
            return null;
        }

        var blob = await _inner.ReadAllBytesAsync(entry!.BlobId, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidDataException($"Backing blob '{entry.BlobId}' is missing.");

        using var cipher = NewCipher(entry.BlobId);
        using var input = new MemoryStream(blob);
        using var output = new MemoryStream();
        await cipher.DecryptAsync(input, output, cancellationToken).ConfigureAwait(false);

        return output.ToArray();
    }

    public async ValueTask WriteAllBytesAsync(
        string path, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default
    )
    {
        EnsureUnlocked();
        var normalized = VfsPath.Normalize(path);

        var blobId = _index!.TryGet(normalized, out var existing) ? existing!.BlobId : NewBlobId();

        using var cipher = NewCipher(blobId);
        using var input = new MemoryStream(data.ToArray());
        using var output = new MemoryStream();
        await cipher.EncryptAsync(input, output, cancellationToken).ConfigureAwait(false);

        await _inner.WriteAllBytesAsync(blobId, output.ToArray(), cancellationToken).ConfigureAwait(false);
        _index.Set(normalized, new VaultIndexEntry(blobId, data.Length, DateTimeOffset.UtcNow));
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)
                   ?? throw new FileNotFoundException($"No file at '{path}'.", path);

        return new MemoryStream(data, writable: false);
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureUnlocked();

        return Task.FromResult<Stream>(new VaultWriteStream(this, path));
    }

    public async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureUnlocked();

        if (!_index!.Remove(VfsPath.Normalize(path), out var entry))
        {
            return false;
        }

        await _inner.DeleteAsync(entry!.BlobId, cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async IAsyncEnumerable<VfsEntry> ListAsync(
        string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        EnsureUnlocked();
        var normalizedPrefix = string.IsNullOrEmpty(prefix) ? null : VfsPath.Normalize(prefix);

        foreach (var (logicalPath, entry) in _index!.Entries)
        {
            if (normalizedPrefix is not null && !logicalPath.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            yield return new VfsEntry(logicalPath, entry.Size, entry.ModifiedUtc);

            await Task.CompletedTask;
        }
    }

    private EntryCipher NewCipher(string blobId)
    {
        return new EntryCipher(VaultKeyDerivation.DeriveSubKey(_masterKey!, "entry:" + blobId), _options.ChunkSize);
    }

    private static string NewBlobId()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    }

    private void FlushIndex()
    {
        var indexKey = VaultKeyDerivation.DeriveSubKey(_masterKey!, "index");
        var encrypted = VaultBlob.Encrypt(indexKey, _index!.Serialize());
        _inner.WriteAllBytesAsync(IndexPath, encrypted).AsTask().GetAwaiter().GetResult();
    }

    private void PruneOrphans()
    {
        var keep = new HashSet<string>(_index!.Entries.Values.Select(e => e.BlobId), StringComparer.Ordinal)
        {
            HeaderPath,
            IndexPath
        };

        var paths = new List<string>();
        var enumerator = _inner.ListAsync().GetAsyncEnumerator();
        try
        {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                paths.Add(enumerator.Current.Path);
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        foreach (var path in paths.Where(p => !keep.Contains(p)))
        {
            _inner.DeleteAsync(path).AsTask().GetAwaiter().GetResult();
        }
    }

    private void EnsureUnlocked()
    {
        if (_masterKey is null)
        {
            throw new InvalidOperationException("The vault is locked.");
        }
    }

    public void Dispose()
    {
        Lock();
    }

    private sealed class VaultWriteStream : MemoryStream
    {
        private readonly CryptoFileSystem _owner;
        private readonly string _path;

        public VaultWriteStream(CryptoFileSystem owner, string path)
        {
            _owner = owner;
            _path = path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _owner.WriteAllBytesAsync(_path, ToArray()).AsTask().GetAwaiter().GetResult();
            }

            base.Dispose(disposing);
        }
    }
}
