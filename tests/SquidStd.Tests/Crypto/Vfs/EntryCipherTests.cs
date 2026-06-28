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
}
