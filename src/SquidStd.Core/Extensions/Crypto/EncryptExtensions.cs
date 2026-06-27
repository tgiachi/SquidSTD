using SquidStd.Core.Utils;

namespace SquidStd.Core.Extensions.Crypto;

/// <summary>
///     String convenience helpers for AES-GCM encryption using a base64-encoded key.
/// </summary>
public static class EncryptExtensions
{
    /// <param name="text">The string to encrypt or decrypt.</param>
    extension(string text)
    {
        /// <summary>
        ///     Encrypts the string with AES-GCM and returns a base64 payload.
        /// </summary>
        /// <param name="base64Key">The base64-encoded key.</param>
        /// <returns>The base64-encoded encrypted payload.</returns>
        public string EncryptString(string base64Key)
        {
            var payload = CryptoUtils.Encrypt(text, Convert.FromBase64String(base64Key));

            return Convert.ToBase64String(payload);
        }

        /// <summary>
        ///     Decrypts a base64 payload produced by <see cref="EncryptString" />.
        /// </summary>
        /// <param name="base64Key">The base64-encoded key.</param>
        /// <returns>The decrypted text.</returns>
        public string DecryptString(string base64Key)
        {
            var payload = Convert.FromBase64String(text);

            return CryptoUtils.Decrypt(payload, Convert.FromBase64String(base64Key));
        }
    }
}
