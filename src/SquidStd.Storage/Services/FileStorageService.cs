using System.Runtime.CompilerServices;
using SquidStd.Storage.Abstractions.Data.Config;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.Internal;

namespace SquidStd.Storage.Services;

/// <summary>
/// Local file-backed binary storage.
/// </summary>
public sealed class FileStorageService : IStorageService
{
    private readonly string _rootDirectory;

    /// <summary>
    /// Initializes local file storage.
    /// </summary>
    /// <param name="config">Storage configuration.</param>
    public FileStorageService(StorageConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.RootDirectory);

        _rootDirectory = Path.GetFullPath(config.RootDirectory);
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(key);

        if (!File.Exists(path))
        {
            return ValueTask.FromResult(false);
        }

        File.Delete(path);

        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(File.Exists(ResolvePath(key)));
    }

    /// <inheritdoc />
    public async ValueTask<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);

        return !File.Exists(path) ? null : await File.ReadAllBytesAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(
        string key,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default
    )
    {
        var path = ResolvePath(key);
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(
            directory ?? _rootDirectory,
            "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp"
        );

        try
        {
            await File.WriteAllBytesAsync(tempPath, data.ToArray(), cancellationToken);
            File.Move(tempPath, path, true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListKeysAsync(
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await Task.CompletedTask;

        if (!Directory.Exists(_rootDirectory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(_rootDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = Path.GetFileName(file);

            if (name.StartsWith('.') && name.EndsWith(".tmp", StringComparison.Ordinal))
            {
                continue;
            }

            var key = Path.GetRelativePath(_rootDirectory, file).Replace('\\', '/');

            if (!string.IsNullOrEmpty(prefix) && !key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            yield return key;
        }
    }

    private string ResolvePath(string key)
        => StoragePathResolver.ResolveFilePath(_rootDirectory, key);
}
