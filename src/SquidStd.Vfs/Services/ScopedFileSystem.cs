using System.Runtime.CompilerServices;
using SquidStd.Vfs.Abstractions;
using SquidStd.Vfs.Abstractions.Data;
using SquidStd.Vfs.Abstractions.Interfaces;

namespace SquidStd.Vfs.Services;

/// <summary>Roots an inner filesystem at a fixed path prefix (a chroot-like view).</summary>
public sealed class ScopedFileSystem : IVirtualFileSystem
{
    private readonly IVirtualFileSystem _inner;
    private readonly string _prefix;

    public ScopedFileSystem(IVirtualFileSystem inner, string prefix)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        _inner = inner;
        _prefix = VfsPath.Normalize(prefix);
    }

    public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        => _inner.ExistsAsync(Scope(path), cancellationToken);

    public ValueTask<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        => _inner.ReadAllBytesAsync(Scope(path), cancellationToken);

    public ValueTask WriteAllBytesAsync(string path, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        => _inner.WriteAllBytesAsync(Scope(path), data, cancellationToken);

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
        => _inner.OpenReadAsync(Scope(path), cancellationToken);

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
        => _inner.OpenWriteAsync(Scope(path), cancellationToken);

    public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        => _inner.DeleteAsync(Scope(path), cancellationToken);

    public async IAsyncEnumerable<VfsEntry> ListAsync(string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var scopedPrefix = string.IsNullOrEmpty(prefix) ? _prefix : Scope(prefix);
        var boundary = _prefix + "/";

        await foreach (var entry in _inner.ListAsync(scopedPrefix, cancellationToken).ConfigureAwait(false))
        {
            if (!entry.Path.StartsWith(boundary, StringComparison.Ordinal))
            {
                continue; // a sibling scope sharing a name prefix (e.g. "tenant10" vs "tenant1") — not ours
            }

            yield return entry with { Path = Unscope(entry.Path) };
        }
    }

    private string Scope(string path)
        => VfsPath.Normalize(_prefix + "/" + path);

    private string Unscope(string path)
        => path.StartsWith(_prefix + "/", StringComparison.Ordinal) ? path[(_prefix.Length + 1)..] : path;
}
