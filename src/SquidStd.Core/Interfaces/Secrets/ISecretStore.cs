namespace SquidStd.Core.Interfaces.Secrets;

/// <summary>
/// Stores encrypted secret values by logical name.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Deletes a secret.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when a secret was deleted; otherwise <c>false</c>.</returns>
    ValueTask<bool> DeleteAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a secret exists.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when the secret exists; otherwise <c>false</c>.</returns>
    ValueTask<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a secret value.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The secret value, or <c>null</c> when it does not exist.</returns>
    ValueTask<string?> GetAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a secret value.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="value">The secret value.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask SetAsync(string name, string value, CancellationToken cancellationToken = default);
}
