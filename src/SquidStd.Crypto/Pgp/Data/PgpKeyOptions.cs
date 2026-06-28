using SquidStd.Crypto.Pgp.Types;

namespace SquidStd.Crypto.Pgp.Data;

/// <summary>
///     Options controlling key generation: algorithm, key size, and an optional validity period.
/// </summary>
public sealed class PgpKeyOptions
{
    /// <summary>Public-key algorithm family. Defaults to RSA.</summary>
    public PgpKeyAlgorithm Algorithm { get; init; } = PgpKeyAlgorithm.Rsa;

    /// <summary>RSA key size in bits. Defaults to 2048.</summary>
    public int KeySizeBits { get; init; } = 2048;

    /// <summary>Optional validity period from creation; null means the key does not expire.</summary>
    public TimeSpan? ExpiresAfter { get; init; }
}
