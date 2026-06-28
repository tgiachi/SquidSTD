using System.Security.Cryptography;

namespace SquidStd.Crypto.Vfs.Internal;

/// <summary>Single-shot AES-GCM for small blobs (the index): layout [nonce(12) | ciphertext | tag(16)].</summary>
internal static class VaultBlob
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] Encrypt(byte[] key, byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        var output = new byte[NonceSize + cipher.Length + TagSize];
        nonce.CopyTo(output, 0);
        cipher.CopyTo(output, NonceSize);
        tag.CopyTo(output, NonceSize + cipher.Length);

        return output;
    }

    public static byte[] Decrypt(byte[] key, byte[] blob)
    {
        var nonce = blob.AsSpan(0, NonceSize);
        var tag = blob.AsSpan(blob.Length - TagSize, TagSize);
        var cipher = blob.AsSpan(NonceSize, blob.Length - NonceSize - TagSize);

        var plaintext = new byte[cipher.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);

        return plaintext;
    }
}
