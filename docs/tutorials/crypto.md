# Crypto (PGP)

Generate a PGP key pair, persist it to disk, encrypt and sign a message, then reload the keyring from disk.

## What you'll build

A host that registers the PGP keyring, service, and a file-backed key store
(`SquidStd.Crypto.Pgp`), generates a key, performs an encrypt-and-sign round-trip, and proves the
keys survive a restart by reloading them from the armored `.asc` files on disk.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Crypto`

## Steps

### 1. Register the PGP services and a file-backed key store

`RegisterPgp` wires the keyring and `IPgpService`; the factory chooses the key store — here a
`FilePgpKeyStore` that reads and writes armored `.asc` files in a directory.

[!code-csharp[](../../samples/SquidStd.Samples.Crypto/Program.cs#step-1)]

### 2. Generate a key and save it to disk

Generate a key pair, import it into the keyring so the service can resolve it by identity, then
persist the keyring to the file-backed store.

[!code-csharp[](../../samples/SquidStd.Samples.Crypto/Program.cs#step-2)]

### 3. Encrypt, sign, decrypt and verify

`EncryptAndSignForAsync` produces armored ciphertext for a recipient; `DecryptAndVerifyAsync`
returns the plaintext together with the signature status.

[!code-csharp[](../../samples/SquidStd.Samples.Crypto/Program.cs#step-3)]

### 4. Reload the keyring from disk

A brand-new `PgpKeyring` loads the same keys back from the store, proving the on-disk material
survives a restart.

[!code-csharp[](../../samples/SquidStd.Samples.Crypto/Program.cs#step-4)]

## Password-based encryption

`PasswordCipher` encrypts a payload directly under a password — no key management required. Argon2id
derives the key, AES-256-GCM seals the data, and the result is a self-describing, versioned envelope
(salt, nonce, tag and KDF cost are embedded). Decryption needs only the password and the blob.

```csharp
using SquidStd.Crypto.Password;
using SquidStd.Crypto.Password.Data;

// Bytes round-trip.
byte[] blob = PasswordCipher.Encrypt(payloadBytes, "correct horse battery staple");
byte[] back = PasswordCipher.Decrypt(blob, "correct horse battery staple");

// Text round-trip — the envelope is base64-encoded, safe to store in config or JSON.
string protectedText = PasswordCipher.EncryptString("a secret", "pw");
string clear         = PasswordCipher.DecryptString(protectedText, "pw");
```

The cost of the Argon2id key derivation defaults to `PbkdfCost.Moderate`. Raise it when protecting
long-lived secrets:

```csharp
// PbkdfCost.Sensitive — slower derivation, stronger resistance to offline attacks.
byte[] strong = PasswordCipher.Encrypt(payloadBytes, "pw", PbkdfCost.Sensitive);
```

A wrong password or tampered data raises `PasswordDecryptionException`. Use `PasswordCipher` for
user-supplied passwords; for app-key or KMS encryption use `CryptoUtils` / `ISecretProtector`
instead.

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Crypto
```

## Next steps

- [Security guide](../articles/guides/security.md)
- [SquidStd.Crypto reference](../articles/crypto.md)
