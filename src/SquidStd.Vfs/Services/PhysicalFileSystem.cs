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
        var full = Path.GetFullPath(Path.Combine(_root, VfsPath.Normalize(path)));

        // Defend against segments that survive normalization yet escape the root (e.g. a Windows
        // drive-rooted segment such as "C:/x", which Path.Combine would resolve outside _root).
        if (full != _root && !full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Path '{path}' escapes the filesystem root.", nameof(path));
        }

        return full;
    }
}
