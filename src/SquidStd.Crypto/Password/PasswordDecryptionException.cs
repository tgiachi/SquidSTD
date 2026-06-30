using System.Security.Cryptography;

namespace SquidStd.Crypto.Password;

/// <summary>Thrown when a password is incorrect or the encrypted payload was corrupted or tampered with.</summary>
public sealed class PasswordDecryptionException : CryptographicException
{
    public PasswordDecryptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
