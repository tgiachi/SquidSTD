using System.Security.Cryptography;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Core.Utils;

public class CryptoUtilsTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrips()
    {
        var key = Convert.FromBase64String(CryptoUtils.GenerateKey());

        var payload = CryptoUtils.Encrypt("hello squid", key);
        var plaintext = CryptoUtils.Decrypt(payload, key);

        Assert.Equal("hello squid", plaintext);
    }

    [Fact]
    public void Encrypt_ProducesDifferentPayloadsPerCall()
    {
        var key = Convert.FromBase64String(CryptoUtils.GenerateKey());

        var first = CryptoUtils.Encrypt("same", key);
        var second = CryptoUtils.Encrypt("same", key);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void GenerateKey_ReturnsKeyOfRequestedSize(int size)
    {
        var key = Convert.FromBase64String(CryptoUtils.GenerateKey(size));

        Assert.Equal(size, key.Length);
    }

    [Fact]
    public void GenerateKey_WhenSizeInvalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => CryptoUtils.GenerateKey(20));
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        var key = Convert.FromBase64String(CryptoUtils.GenerateKey());
        var otherKey = Convert.FromBase64String(CryptoUtils.GenerateKey());
        var payload = CryptoUtils.Encrypt("secret", key);

        Assert.Throws<AuthenticationTagMismatchException>(() => CryptoUtils.Decrypt(payload, otherKey));
    }

    [Fact]
    public void Decrypt_WhenPayloadTooShort_Throws()
    {
        var key = Convert.FromBase64String(CryptoUtils.GenerateKey());

        Assert.Throws<ArgumentException>(() => CryptoUtils.Decrypt([1, 2, 3], key));
    }
}
