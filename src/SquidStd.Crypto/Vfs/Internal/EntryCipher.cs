using System.Buffers.Binary;
using System.Security.Cryptography;

namespace SquidStd.Crypto.Vfs.Internal;

/// <summary>Streams a payload as chunked AES-GCM records: [len(4) | nonce(12) | ciphertext | tag(16)] per chunk.</summary>
internal sealed class EntryCipher : IDisposable
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly AesGcm _aes;
    private readonly int _chunkSize;

    public EntryCipher(byte[] key, int chunkSize)
    {
        _aes = new AesGcm(key, TagSize);
        _chunkSize = chunkSize;
    }

    public async Task EncryptAsync(Stream plaintext, Stream output, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[_chunkSize];

        while (true)
        {
            var read = await ReadFullAsync(plaintext, buffer, cancellationToken).ConfigureAwait(false);
            await WriteRecordAsync(output, buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                break; // terminator record written
            }
        }
    }

    public async Task DecryptAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        var header = new byte[4 + NonceSize];

        while (true)
        {
            await ReadExactAsync(input, header, cancellationToken).ConfigureAwait(false);
            var length = BinaryPrimitives.ReadInt32BigEndian(header);
            var nonce = header.AsMemory(4, NonceSize);

            // The length prefix is read from the (unauthenticated) record header. Bound it before
            // allocating so a corrupt or tampered vault cannot cause OOM or a negative-size crash.
            if (length < 0 || length > _chunkSize)
            {
                throw new InvalidDataException("Encrypted entry record length is out of range.");
            }

            var cipher = new byte[length];
            await ReadExactAsync(input, cipher, cancellationToken).ConfigureAwait(false);
            var tag = new byte[TagSize];
            await ReadExactAsync(input, tag, cancellationToken).ConfigureAwait(false);

            if (length == 0)
            {
                _aes.Decrypt(nonce.Span, ReadOnlySpan<byte>.Empty, tag, Span<byte>.Empty);
                break;
            }

            var plain = new byte[length];
            _aes.Decrypt(nonce.Span, cipher, tag, plain);
            await output.WriteAsync(plain, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteRecordAsync(Stream output, ReadOnlyMemory<byte> plain, CancellationToken cancellationToken)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];
        _aes.Encrypt(nonce, plain.Span, cipher, tag);

        var header = new byte[4 + NonceSize];
        BinaryPrimitives.WriteInt32BigEndian(header, plain.Length);
        nonce.CopyTo(header.AsSpan(4));

        await output.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await output.WriteAsync(cipher, cancellationToken).ConfigureAwait(false);
        await output.WriteAsync(tag, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ReadFullAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;

        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;

        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                throw new EndOfStreamException("Encrypted entry is truncated.");
            }

            total += read;
        }
    }

    public void Dispose()
    {
        _aes.Dispose();
    }
}
