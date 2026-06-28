using SquidStd.Crypto.Pgp.Services;
using SquidStd.Tests.Crypto.Pgp.Support;

namespace SquidStd.Tests.Crypto.Pgp;

[Collection("PgpKeys")]
public class PgpKeyringTests
{
    private readonly PgpTestKeys _keys;

    public PgpKeyringTests(PgpTestKeys keys)
    {
        _keys = keys;
    }

    [Fact]
    public void Import_PublicBlock_FindableByIdentityKeyIdAndFingerprint()
    {
        var keyring = new PgpKeyring();
        var key = keyring.Import(_keys.AlicePublic);

        Assert.Same(key, keyring.Find(PgpTestKeys.AliceIdentity));
        Assert.Same(key, keyring.Find(key.KeyId));
        Assert.Same(key, keyring.Find(key.Fingerprint));
        Assert.True(keyring.Contains(PgpTestKeys.AliceIdentity));
        Assert.False(key.HasSecret);
    }

    [Fact]
    public void Import_SecretBlock_SetsHasSecret()
    {
        var keyring = new PgpKeyring();

        var key = keyring.Import(_keys.AlicePrivate);

        Assert.True(key.HasSecret);
    }

    [Fact]
    public void Remove_ByIdentity_RemovesKey()
    {
        var keyring = new PgpKeyring();
        keyring.Import(_keys.AlicePublic);

        Assert.True(keyring.Remove(PgpTestKeys.AliceIdentity));
        Assert.Null(keyring.Find(PgpTestKeys.AliceIdentity));
        Assert.Empty(keyring.Keys);
    }
}
