namespace SquidStd.Crypto.Pgp.Data;

/// <summary>
///     Outcome of decrypt-and-verify: the recovered plaintext, whether the message carried a signature, and
///     whether that signature validated against a keyring public key.
/// </summary>
public sealed record PgpDecryptionResult(byte[] Data, bool IsSigned, bool IsValid);
