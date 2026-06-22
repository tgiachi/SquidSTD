namespace SquidStd.Core.Interfaces.Secrets;

/// <summary>
/// Protects and unprotects secret payloads.
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// Protects a plaintext payload.
    /// </summary>
    /// <param name="plaintext">Plaintext bytes to protect.</param>
    /// <returns>Protected payload bytes.</returns>
    byte[] Protect(byte[] plaintext);

    /// <summary>
    /// Unprotects a protected payload.
    /// </summary>
    /// <param name="protectedData">Protected payload bytes.</param>
    /// <returns>Plaintext bytes.</returns>
    byte[] Unprotect(byte[] protectedData);
}
