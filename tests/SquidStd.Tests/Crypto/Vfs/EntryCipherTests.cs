using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using SquidStd.Crypto.Vfs.Internal;

namespace SquidStd.Tests.Crypto.Vfs;

public class EntryCipherTests
{
    [Fact]
    public async Task EncryptThenDecrypt_RoundTripsAcrossChunkBoundaries()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var payload = RandomNumberGenerator.GetBytes(200_000); // > 3 chunks at 64 KiB

        using var cipher = new EntryCipher(key, chunkSize: 65536);
        using var encrypted = new MemoryStream();
        await cipher.EncryptAsync(new MemoryStream(payload), encrypted);

        encrypted.Position = 0;
        using var decrypted = new MemoryStream();
        await cipher.DecryptAsync(encrypted, decrypted);

        Assert.Equal(payload, decrypted.ToArray());
    }

    [Fact]
    public async Task Decrypt_WrongKey_Throws()
    {
        var payload = Encoding.UTF8.GetBytes("secret");
        using var enc = new EntryCipher(RandomNumberGenerator.GetBytes(32), 65536);
        using var encrypted = new MemoryStream();
        await enc.EncryptAsync(new MemoryStream(payload), encrypted);

        using var wrong = new EntryCipher(RandomNumberGenerator.GetBytes(32), 65536);
        encrypted.Position = 0;
        await Assert.ThrowsAsync<AuthenticationTagMismatchException>(() => wrong.DecryptAsync(encrypted, new MemoryStream())
        );
    }

    [Fact]
    public async Task Decrypt_RecordLengthExceedingChunkSize_Throws_WithoutAllocating()
    {
        const int chunkSize = 65536;

        // A record header whose unauthenticated length prefix claims a chunk larger than the cipher's
        // configured chunk size must be rejected before allocation, not used to size a buffer.
        var header = new byte[4 + 12];
        BinaryPrimitives.WriteInt32BigEndian(header, chunkSize + 1);

        using var cipher = new EntryCipher(RandomNumberGenerator.GetBytes(32), chunkSize);
        await Assert.ThrowsAsync<InvalidDataException>(() => cipher.DecryptAsync(new MemoryStream(header), new MemoryStream())
        );
    }
}
