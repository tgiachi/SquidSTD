using PgpCore;

namespace SquidStd.Tests.Crypto.Pgp.Support;

/// <summary>
///     Generates two throwaway armored RSA-2048 key pairs once for the whole assembly. Key generation is
///     expensive, so tests share this fixture via <see cref="PgpKeysCollection" />.
/// </summary>
public sealed class PgpTestKeys
{
    public const string AlicePassphrase = "alice-pw";
    public const string BobPassphrase = "bob-pw";
    public const string AliceIdentity = "alice@squidstd.test";
    public const string BobIdentity = "bob@squidstd.test";

    public PgpTestKeys()
    {
        (AlicePublic, AlicePrivate) = Generate(AliceIdentity, AlicePassphrase);
        (BobPublic, BobPrivate) = Generate(BobIdentity, BobPassphrase);
    }

    public string AlicePublic { get; }

    public string AlicePrivate { get; }

    public string BobPublic { get; }

    public string BobPrivate { get; }

    private static (string Public, string Private) Generate(string identity, string passphrase)
    {
        var pgp = new PGP();
        using var pub = new MemoryStream();
        using var priv = new MemoryStream();
        pgp.GenerateKey(pub, priv, identity, passphrase, 2048);

        return (ReadAll(pub), ReadAll(priv));
    }

    private static string ReadAll(MemoryStream stream)
    {
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
