namespace SquidStd.Core.Interfaces.Storage;

/// <summary>
/// Stores typed objects by logical key.
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    /// Deletes a stored object.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when a payload was deleted; otherwise <c>false</c>.</returns>
    ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an object exists.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when the object exists; otherwise <c>false</c>.</returns>
    ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a stored object.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="T">The object type.</typeparam>
    /// <returns>The object, or <c>null</c> when it does not exist.</returns>
    ValueTask<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an object.
    /// </summary>
    /// <param name="key">The logical storage key.</param>
    /// <param name="value">The object value.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="T">The object type.</typeparam>
    ValueTask SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}
