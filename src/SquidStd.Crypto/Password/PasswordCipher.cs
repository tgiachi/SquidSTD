using System.Security.Cryptography;
using System.Text;
using SquidStd.Crypto.Password.Data;
using SquidStd.Crypto.Password.Internal;

namespace SquidStd.Crypto.Password;

/// <summary>
/// Password-based authenticated encryption. Argon2id derives a key from the password, AES-256-GCM encrypts
/// the payload, and the result is a self-describing envelope carrying the salt, nonce, tag and KDF cost — so
/// decryption needs only the password and the blob.
/// </summary>
public static class PasswordCipher
{
    /// <summary>Encrypts <paramref name="plaintext" /> under <paramref name="password" />.</summary>
    public static byte[] Encrypt(byte[] plaintext, string password, PbkdfCost? cost = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentException.ThrowIfNullOrEmpty(password);

        cost ??= PbkdfCost.Moderate;

        var salt = RandomNumberGenerator.GetBytes(PasswordEnvelopeCodec.SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(PasswordEnvelopeCodec.NonceSize);
        var key = PasswordKeyDerivation.DeriveKey(password, salt, cost.Iterations, cost.MemoryKib, cost.Parallelism);

        var aad = PasswordEnvelopeCodec.BuildAad(
            PasswordEnvelopeCodec.Version, cost.Iterations, cost.MemoryKib, cost.Parallelism, salt, nonce
        );
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[PasswordEnvelopeCodec.TagSize];

        using (var aes = new AesGcm(key, PasswordEnvelopeCodec.TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
        }

        var envelope = new PasswordEnvelope(
            PasswordEnvelopeCodec.Version, cost.Iterations, cost.MemoryKib, cost.Parallelism, salt, nonce, tag, ciphertext
        );

        return PasswordEnvelopeCodec.Encode(envelope);
    }

    /// <summary>Decrypts an envelope produced by <see cref="Encrypt" />.</summary>
    public static byte[] Decrypt(byte[] envelope, string password)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrEmpty(password);

        var parsed = PasswordEnvelopeCodec.Decode(envelope);
        var key = PasswordKeyDerivation.DeriveKey(
            password, parsed.Salt, parsed.Iterations, parsed.MemoryKib, parsed.Parallelism
        );
        var aad = PasswordEnvelopeCodec.AadOf(envelope);
        var plaintext = new byte[parsed.Ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, PasswordEnvelopeCodec.TagSize);
            aes.Decrypt(parsed.Nonce, parsed.Ciphertext, parsed.Tag, plaintext, aad);
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new PasswordDecryptionException("The password is incorrect or the data has been corrupted.", ex);
        }

        return plaintext;
    }

    /// <summary>Encrypts a UTF-8 string and returns the envelope as base64.</summary>
    public static string EncryptString(string text, string password, PbkdfCost? cost = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(text), password, cost));
    }

    /// <summary>Decrypts a base64 envelope produced by <see cref="EncryptString" /> back to its UTF-8 string.</summary>
    public static string DecryptString(string base64, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(base64);

        byte[] envelope;

        try
        {
            envelope = Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Input is not valid base64.", ex);
        }

        return Encoding.UTF8.GetString(Decrypt(envelope, password));
    }
}
