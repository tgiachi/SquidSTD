using System.Security.Cryptography;
using System.Text;

namespace SquidStd.Core.Utils;

/// <summary>
///     Authenticated symmetric encryption helpers built on AES-GCM. Produced payloads are laid out as
///     <c>nonce (12 bytes) | tag (16 bytes) | ciphertext</c>.
/// </summary>
public static class CryptoUtils
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    ///     Generates a random symmetric key and returns it as a base64 string.
    /// </summary>
    /// <param name="sizeInBytes">Key length in bytes; must be 16, 24, or 32.</param>
    /// <returns>The base64-encoded key.</returns>
    public static string GenerateKey(int sizeInBytes = 32)
    {
        if (sizeInBytes is not (16 or 24 or 32))
        {
            throw new ArgumentException("Key size must be 16, 24, or 32 bytes.", nameof(sizeInBytes));
        }

        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(sizeInBytes));
    }

    /// <summary>
    ///     Encrypts a UTF-8 string with AES-GCM under the supplied key.
    /// </summary>
    /// <param name="plaintext">The text to encrypt.</param>
    /// <param name="key">The 16, 24, or 32 byte key.</param>
    /// <returns>The <c>nonce | tag | ciphertext</c> payload.</returns>
    public static byte[] Encrypt(string plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(key);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        ciphertext.CopyTo(payload, NonceSize + TagSize);

        return payload;
    }

    /// <summary>
    ///     Decrypts a payload produced by <see cref="Encrypt" /> back into its UTF-8 string.
    /// </summary>
    /// <param name="payload">The <c>nonce | tag | ciphertext</c> payload.</param>
    /// <param name="key">The key used to encrypt the payload.</param>
    /// <returns>The decrypted text.</returns>
    public static string Decrypt(byte[] payload, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(key);

        if (payload.Length < NonceSize + TagSize)
        {
            throw new ArgumentException("Payload is too short to contain a nonce and tag.", nameof(payload));
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
