using System.Security.Cryptography;
using System.Text;
using SquidStd.Crypto.Vfs.Internal;

namespace SquidStd.Tests.Crypto.Vfs;

public class VaultBlobTests
{
    [Fact]
    public void EncryptThenDecrypt_RoundTrips()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("vault index payload");

        var blob = VaultBlob.Encrypt(key, plaintext);

        Assert.Equal(plaintext, VaultBlob.Decrypt(key, blob));
    }

    [Fact]
    public void Decrypt_BlobShorterThanNonceAndTag_Throws()
    {
        var key = RandomNumberGenerator.GetBytes(32);

        // A corrupt or truncated blob with fewer than nonce+tag bytes must fail with a clear data
        // error instead of an opaque negative-length slice exception.
        var truncated = new byte[12 + 16 - 1];

        Assert.Throws<InvalidDataException>(() => VaultBlob.Decrypt(key, truncated));
    }
}
