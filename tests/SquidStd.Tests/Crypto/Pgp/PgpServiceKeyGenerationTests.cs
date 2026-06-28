using SquidStd.Crypto.Pgp.Services;

namespace SquidStd.Tests.Crypto.Pgp;

public class PgpServiceKeyGenerationTests
{
    [Fact]
    public void GenerateKey_ReturnsKeyWithMetadataAndSecret()
    {
        var keyring = new PgpKeyring();
        var service = new PgpService(keyring);

        var key = service.GenerateKey("carol@squidstd.test", "carol-pw");

        Assert.Equal("carol@squidstd.test", key.Identity);
        Assert.False(string.IsNullOrWhiteSpace(key.KeyId));
        Assert.False(string.IsNullOrWhiteSpace(key.Fingerprint));
        Assert.True(key.HasSecret);
    }
}
