using SquidStd.Core.Data.Storage;
using SquidStd.Core.Interfaces.Storage;
using SquidStd.Services.Core.Services.Internal;

namespace SquidStd.Services.Core.Services.Storage;

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

    private string ResolvePath(string key)
        => StoragePathResolver.ResolveFilePath(_rootDirectory, key);
}
