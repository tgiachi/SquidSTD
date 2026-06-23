namespace SquidStd.Storage.Abstractions.Interfaces;

/// <summary>
/// Stores binary payloads by logical key.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Deletes a stored payload.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when a payload was deleted; otherwise <c>false</c>.</returns>
    ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a payload exists.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when the payload exists; otherwise <c>false</c>.</returns>
    ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a binary payload.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The payload, or <c>null</c> when it does not exist.</returns>
    ValueTask<byte[]?> LoadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a binary payload atomically.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="data">The payload to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask SaveAsync(string key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates stored keys, optionally filtered by prefix.
    /// </summary>
    /// <param name="prefix">Optional key prefix; <c>null</c> or empty returns all keys.</param>
    /// <param name="cancellationToken">Token used to cancel the enumeration.</param>
    /// <returns>An async sequence of storage keys.</returns>
    IAsyncEnumerable<string> ListKeysAsync(string? prefix = null, CancellationToken cancellationToken = default);
}
