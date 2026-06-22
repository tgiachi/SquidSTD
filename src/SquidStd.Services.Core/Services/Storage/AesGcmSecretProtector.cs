using System.Security.Cryptography;
using System.Text;
using Serilog;
using SquidStd.Core.Data.Storage;
using SquidStd.Core.Interfaces.Secrets;

namespace SquidStd.Services.Core.Services.Storage;

/// <summary>
/// Protects secrets using AES-GCM and a key supplied by environment variable.
/// </summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const string EnvelopePrefix = "SQUIDSTD-AESGCM-V1";
    private const string DefaultKeyMaterial = "SquidStd.DefaultDevelopmentSecretsKey.v1";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    /// <summary>
    /// Initializes the AES-GCM secret protector.
    /// </summary>
    /// <param name="config">Secret storage configuration.</param>
    public AesGcmSecretProtector(SecretsConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _key = ResolveKey(config.KeyEnvironmentVariable);
    }

    /// <inheritdoc />
    public byte[] Protect(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var envelope = string.Join(
            ":",
            EnvelopePrefix,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext)
        );

        return Encoding.UTF8.GetBytes(envelope);
    }

    /// <inheritdoc />
    public byte[] Unprotect(byte[] protectedData)
    {
        ArgumentNullException.ThrowIfNull(protectedData);

        var envelope = Encoding.UTF8.GetString(protectedData);
        var parts = envelope.Split(':');

        if (parts.Length != 4 || !string.Equals(parts[0], EnvelopePrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("Unsupported secret payload format.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private static byte[] ResolveKey(string environmentVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentVariable);

        var value = Environment.GetEnvironmentVariable(environmentVariable);

        if (string.IsNullOrWhiteSpace(value))
        {
            Log.Warning(
                "Secret key environment variable {EnvironmentVariable} is not set. Using the default development secret key.",
                environmentVariable
            );

            return CreateDefaultKey();
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Secret key environment variable must contain a base64 value.", ex);
        }

        return key.Length is 16 or 24 or 32
                   ? key
                   : throw new InvalidOperationException("Secret key must be 16, 24, or 32 bytes after base64 decoding.");
    }

    private static byte[] CreateDefaultKey()
        => SHA256.HashData(Encoding.UTF8.GetBytes(DefaultKeyMaterial));
}
