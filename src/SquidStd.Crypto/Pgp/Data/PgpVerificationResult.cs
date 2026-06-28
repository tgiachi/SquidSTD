namespace SquidStd.Crypto.Pgp.Data;

/// <summary>
///     Outcome of verifying a signed message: whether the signature validated against a keyring public key,
///     plus the recovered content. PgpCore reports pass/fail only — no signer attribution is exposed.
/// </summary>
public sealed record PgpVerificationResult(bool IsValid, byte[] Data);
