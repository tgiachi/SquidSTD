using System.IO.Compression;
using System.Runtime.CompilerServices;
using SquidStd.Vfs.Abstractions.Data;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Abstractions;

namespace SquidStd.Vfs.Services;

/// <summary>A virtual filesystem backed by a single zip archive opened in update mode.</summary>
public sealed class ZipFileSystem : IVirtualFileSystem, IAsyncDisposable, IDisposable
{
    private readonly ZipArchive _archive;
    private readonly FileStream _file;

    public ZipFileSystem(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        _archive = new ZipArchive(_file, ZipArchiveMode.Update, leaveOpen: true);
    }

    public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_archive.GetEntry(VfsPath.Normalize(path)) is not null);
    }

    public async ValueTask<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        var entry = _archive.GetEntry(VfsPath.Normalize(path));

        if (entry is null)
        {
            return null;
        }

        await using var stream = entry.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        return buffer.ToArray();
    }

    public async ValueTask WriteAllBytesAsync(
        string path, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default
    )
    {
        var name = VfsPath.Normalize(path);
        _archive.GetEntry(name)?.Delete();
        var entry = _archive.CreateEntry(name, CompressionLevel.Optimal);

        await using var stream = entry.Open();
        await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
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
        var entry = _archive.GetEntry(VfsPath.Normalize(path));

        if (entry is null)
        {
            return ValueTask.FromResult(false);
        }

        entry.Delete();

        return ValueTask.FromResult(true);
    }

    public async IAsyncEnumerable<VfsEntry> ListAsync(
        string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var normalizedPrefix = string.IsNullOrEmpty(prefix) ? null : VfsPath.Normalize(prefix);

        foreach (var entry in _archive.Entries)
        {
            if (normalizedPrefix is not null && !entry.FullName.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            yield return new VfsEntry(entry.FullName, entry.Length, entry.LastWriteTime);

            await Task.CompletedTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _archive.Dispose();
        await _file.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _archive.Dispose();
        _file.Dispose();
    }

    private sealed class WriteBackStream : MemoryStream
    {
        private readonly ZipFileSystem _owner;
        private readonly string _path;

        public WriteBackStream(ZipFileSystem owner, string path)
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
