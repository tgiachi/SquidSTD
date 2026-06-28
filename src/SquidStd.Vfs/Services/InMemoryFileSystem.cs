using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SquidStd.Vfs.Abstractions.Data;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Abstractions;

namespace SquidStd.Vfs.Services;

/// <summary>An in-memory virtual filesystem. Ephemeral; useful for tests and as a backend decorator target.</summary>
public sealed class InMemoryFileSystem : IVirtualFileSystem
{
    private readonly ConcurrentDictionary<string, (byte[] Data, DateTimeOffset Modified)> _files = new(
        StringComparer.Ordinal
    );

    public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_files.ContainsKey(VfsPath.Normalize(path)));
    }

    public ValueTask<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_files.TryGetValue(VfsPath.Normalize(path), out var entry) ? entry.Data : null);
    }

    public ValueTask WriteAllBytesAsync(
        string path, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default
    )
    {
        _files[VfsPath.Normalize(path)] = (data.ToArray(), DateTimeOffset.UtcNow);

        return ValueTask.CompletedTask;
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)
                   ?? throw new FileNotFoundException($"No file at '{path}'.", path);

        return new MemoryStream(data, writable: false);
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(new WriteBackStream(this, path));
    }

    public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_files.TryRemove(VfsPath.Normalize(path), out _));
    }

    public async IAsyncEnumerable<VfsEntry> ListAsync(
        string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var normalizedPrefix = string.IsNullOrEmpty(prefix) ? null : VfsPath.Normalize(prefix);

        foreach (var (path, entry) in _files)
        {
            if (normalizedPrefix is not null && !path.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            yield return new VfsEntry(path, entry.Data.Length, entry.Modified);

            await Task.CompletedTask;
        }
    }

    private sealed class WriteBackStream : MemoryStream
    {
        private readonly InMemoryFileSystem _owner;
        private readonly string _path;

        public WriteBackStream(InMemoryFileSystem owner, string path)
        {
            _owner = owner;
            _path = path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _owner._files[VfsPath.Normalize(_path)] = (ToArray(), DateTimeOffset.UtcNow);
            }

            base.Dispose(disposing);
        }
    }
}
