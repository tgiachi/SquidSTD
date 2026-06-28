using System.Globalization;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using PgpCore;
using SquidStd.Crypto.Pgp.Data;
using SquidStd.Crypto.Pgp.Types;

namespace SquidStd.Crypto.Pgp.Internal;

/// <summary>
///     Builds a <see cref="PgpKey" /> from armored key material by reading metadata off the public master key.
///     Shared by key generation, keyring import, and key-store loading.
/// </summary>
internal static class PgpKeyFactory
{
    public static PgpKey FromArmored(string publicArmored, string? privateArmored)
    {
        var master = new EncryptionKeys(publicArmored).MasterKey;

        var identity = FirstUserId(master);
        var keyId = master.KeyId.ToString("X16", CultureInfo.InvariantCulture);
        var fingerprint = Convert.ToHexString(master.GetFingerprint());
        var createdUtc = new DateTimeOffset(DateTime.SpecifyKind(master.CreationTime, DateTimeKind.Utc));

        var validSeconds = master.GetValidSeconds();
        DateTimeOffset? expiresUtc = validSeconds > 0 ? createdUtc.AddSeconds(validSeconds) : null;

        return new PgpKey(
            identity,
            keyId,
            fingerprint,
            publicArmored,
            privateArmored,
            createdUtc,
            expiresUtc,
            MapAlgorithm(master.Algorithm)
        );
    }

    private static string FirstUserId(PgpPublicKey master)
    {
        foreach (var userId in master.GetUserIds())
        {
            if (userId is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static PgpKeyAlgorithm MapAlgorithm(PublicKeyAlgorithmTag tag)
    {
        return tag switch
        {
            PublicKeyAlgorithmTag.RsaGeneral or PublicKeyAlgorithmTag.RsaEncrypt or PublicKeyAlgorithmTag.RsaSign =>
                PgpKeyAlgorithm.Rsa,
            _ => PgpKeyAlgorithm.Rsa
        };
    }
}
