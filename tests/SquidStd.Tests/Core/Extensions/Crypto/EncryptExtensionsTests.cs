using SquidStd.Core.Extensions.Crypto;
using SquidStd.Core.Extensions.Strings;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Core.Extensions.Crypto;

public class EncryptExtensionsTests
{
    [Fact]
    public void EncryptString_DecryptString_RoundTrips()
    {
        var key = CryptoUtils.GenerateKey();

        var encrypted = "top secret".EncryptString(key);
        var decrypted = encrypted.DecryptString(key);

        Assert.NotEqual("top secret", encrypted);
        Assert.Equal("top secret", decrypted);
    }

    [Fact]
    public void EncryptString_ProducesBase64Payload()
    {
        var key = CryptoUtils.GenerateKey();

        var encrypted = "value".EncryptString(key);

        Assert.True(encrypted.IsBase64String());
    }
}
