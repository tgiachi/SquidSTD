using SquidStd.Crypto.Pgp.Types;

namespace SquidStd.Crypto.Pgp.Data;

/// <summary>
///     An OpenPGP key as held in the keyring: identity, key id, fingerprint, the armored public block, and
///     optionally the armored secret block. Metadata is derived from the public key material.
/// </summary>
public sealed record PgpKey(
    string Identity,
    string KeyId,
    string Fingerprint,
    string PublicArmored,
    string? PrivateArmored,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ExpiresUtc,
    PgpKeyAlgorithm Algorithm)
{
    /// <summary>Whether this key carries secret material (can decrypt and sign).</summary>
    public bool HasSecret => PrivateArmored is not null;
}
