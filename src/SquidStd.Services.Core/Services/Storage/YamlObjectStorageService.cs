using System.Text;
using SquidStd.Core.Interfaces.Storage;
using SquidStd.Core.Yaml;

namespace SquidStd.Services.Core.Services.Storage;

/// <summary>
/// YAML object storage built on top of binary storage.
/// </summary>
public sealed class YamlObjectStorageService : IObjectStorageService
{
    private readonly IStorageService _storageService;

    /// <summary>
    /// Initializes YAML object storage.
    /// </summary>
    /// <param name="storageService">Underlying binary storage service.</param>
    public YamlObjectStorageService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _storageService.DeleteAsync(key, cancellationToken);

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => _storageService.ExistsAsync(key, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await _storageService.LoadAsync(key, cancellationToken);

        if (data is null)
        {
            return default;
        }

        var yaml = Encoding.UTF8.GetString(data);

        return YamlUtils.Deserialize<T>(yaml);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        var yaml = YamlUtils.Serialize(value);
        await _storageService.SaveAsync(key, Encoding.UTF8.GetBytes(yaml), cancellationToken);
    }
}
