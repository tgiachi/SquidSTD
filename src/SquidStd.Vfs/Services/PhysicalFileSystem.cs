using System.Runtime.CompilerServices;
using SquidStd.Vfs.Abstractions.Data;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Abstractions;

namespace SquidStd.Vfs.Services;

/// <summary>A virtual filesystem mapped onto a real directory tree.</summary>
public sealed class PhysicalFileSystem : IVirtualFileSystem
{
    private readonly string _root;

    public PhysicalFileSystem(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(File.Exists(Resolve(path)));
    }

    public async ValueTask<byte[]?> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);

        return File.Exists(full)
            ? await File.ReadAllBytesAsync(full, cancellationToken).ConfigureAwait(false)
            : null;
    }

    public async ValueTask WriteAllBytesAsync(
        string path, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default
    )
    {
        var full = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, data, cancellationToken).ConfigureAwait(false);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);

        if (!File.Exists(full))
        {
            throw new FileNotFoundException($"No file at '{path}'.", path);
        }

        return Task.FromResult<Stream>(File.OpenRead(full));
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        return Task.FromResult<Stream>(File.Create(full));
    }

    public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);

        if (!File.Exists(full))
        {
            return ValueTask.FromResult(false);
        }

        File.Delete(full);

        return ValueTask.FromResult(true);
    }

    public async IAsyncEnumerable<VfsEntry> ListAsync(
        string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (!Directory.Exists(_root))
        {
            yield break;
        }

        var normalizedPrefix = string.IsNullOrEmpty(prefix) ? null : VfsPath.Normalize(prefix);

        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var logical = Path.GetRelativePath(_root, file).Replace('\\', '/');

            if (normalizedPrefix is not null && !logical.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var info = new FileInfo(file);
            yield return new VfsEntry(logical, info.Length, info.LastWriteTimeUtc);

            await Task.CompletedTask;
        }
    }

    private string Resolve(string path)
    {
        return Path.Combine(_root, VfsPath.Normalize(path));
    }
}
