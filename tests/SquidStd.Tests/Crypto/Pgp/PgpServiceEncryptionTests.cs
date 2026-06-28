using System.Text;
using SquidStd.Crypto.Pgp.Services;
using SquidStd.Tests.Crypto.Pgp.Support;

namespace SquidStd.Tests.Crypto.Pgp;

[Collection("PgpKeys")]
public class PgpServiceEncryptionTests
{
    private readonly PgpTestKeys _keys;

    public PgpServiceEncryptionTests(PgpTestKeys keys)
    {
        _keys = keys;
    }

    [Fact]
    public async Task EncryptFor_ThenDecrypt_RoundTripsBytes()
    {
        var keyring = new PgpKeyring();
        keyring.Import(_keys.AlicePublic);   // recipient public only for encryption
        keyring.Import(_keys.AlicePrivate);  // secret for decryption (replaces same key id, now HasSecret)
        var service = new PgpService(keyring);
        var payload = Encoding.UTF8.GetBytes("squid secret message");

        var armored = await service.EncryptForAsync(PgpTestKeys.AliceIdentity, payload);
        var plain = await service.DecryptAsync(armored, PgpTestKeys.AlicePassphrase);

        Assert.StartsWith("-----BEGIN PGP MESSAGE-----", armored);
        Assert.Equal(payload, plain);
    }

    [Fact]
    public async Task EncryptFor_UnknownRecipient_ThrowsKeyNotFound()
    {
        var service = new PgpService(new PgpKeyring());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.EncryptForAsync("nobody@squidstd.test", Encoding.UTF8.GetBytes("x"))
        );
    }

    [Fact]
    public async Task EncryptFor_ThenDecrypt_StreamOverloadsRoundTripLargePayload()
    {
        var keyring = new PgpKeyring();
        keyring.Import(_keys.AlicePrivate);
        var service = new PgpService(keyring);
        var payload = Encoding.UTF8.GetBytes(new string('z', 8192));

        using var plaintext = new MemoryStream(payload);
        using var ciphertext = new MemoryStream();
        await service.EncryptForAsync(PgpTestKeys.AliceIdentity, plaintext, ciphertext);

        ciphertext.Position = 0;
        using var recovered = new MemoryStream();
        await service.DecryptAsync(ciphertext, recovered, PgpTestKeys.AlicePassphrase);

        Assert.Equal(payload, recovered.ToArray());
    }
}
