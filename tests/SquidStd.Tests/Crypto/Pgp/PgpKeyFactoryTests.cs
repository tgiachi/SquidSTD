using SquidStd.Crypto.Pgp.Internal;
using SquidStd.Crypto.Pgp.Types;
using SquidStd.Tests.Crypto.Pgp.Support;

namespace SquidStd.Tests.Crypto.Pgp;

[Collection("PgpKeys")]
public class PgpKeyFactoryTests
{
    private readonly PgpTestKeys _keys;

    public PgpKeyFactoryTests(PgpTestKeys keys)
    {
        _keys = keys;
    }

    [Fact]
    public void FromArmored_PublicOnly_ParsesMetadataWithoutSecret()
    {
        var key = PgpKeyFactory.FromArmored(_keys.AlicePublic, null);

        Assert.Equal(PgpTestKeys.AliceIdentity, key.Identity);
        Assert.False(string.IsNullOrWhiteSpace(key.KeyId));
        Assert.False(string.IsNullOrWhiteSpace(key.Fingerprint));
        Assert.Equal(PgpKeyAlgorithm.Rsa, key.Algorithm);
        Assert.False(key.HasSecret);
    }

    [Fact]
    public void FromArmored_WithSecret_SetsHasSecret()
    {
        var key = PgpKeyFactory.FromArmored(_keys.AlicePublic, _keys.AlicePrivate);

        Assert.True(key.HasSecret);
        Assert.Equal(_keys.AlicePrivate, key.PrivateArmored);
    }
}
