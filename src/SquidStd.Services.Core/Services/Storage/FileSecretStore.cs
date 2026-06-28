using System.Runtime.CompilerServices;
using System.Text;
using SquidStd.Core.Data.Storage;
using SquidStd.Core.Interfaces.Secrets;
using SquidStd.Storage.Abstractions.Data.Config;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.Services;

namespace SquidStd.Services.Core.Services.Storage;

/// <summary>
///     File-backed encrypted secret store.
/// </summary>
public sealed class FileSecretStore : ISecretStore
{
    private readonly ISecretProtector _secretProtector;
    private readonly IStorageService _storageService;

    /// <summary>
    ///     Initializes the encrypted file secret store.
    /// </summary>
    /// <param name="config">Secret storage configuration.</param>
    /// <param name="secretProtector">Secret protector used for encryption.</param>
    public FileSecretStore(SecretsConfig config, ISecretProtector secretProtector)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(secretProtector);

        _secretProtector = secretProtector;
        _storageService = new FileStorageService(new StorageConfig { RootDirectory = config.RootDirectory });
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        return _storageService.DeleteAsync(ToStorageKey(name), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return _storageService.ExistsAsync(ToStorageKey(name), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var protectedData = await _storageService.LoadAsync(ToStorageKey(name), cancellationToken);

        if (protectedData is null)
        {
            return null;
        }

        var plaintext = _secretProtector.Unprotect(protectedData);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        var plaintext = Encoding.UTF8.GetBytes(value);
        var protectedData = _secretProtector.Protect(plaintext);

        await _storageService.SaveAsync(ToStorageKey(name), protectedData, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListNamesAsync(
        string? prefix = null, [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        const string suffix = ".secret";
        var keyPrefix = string.IsNullOrEmpty(prefix) ? null : prefix;

        await foreach (var key in _storageService.ListKeysAsync(keyPrefix, cancellationToken).ConfigureAwait(false))
        {
            if (key.EndsWith(suffix, StringComparison.Ordinal))
            {
                yield return key[..^suffix.Length];
            }
        }
    }

    private static string ToStorageKey(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return name + ".secret";
    }
}
