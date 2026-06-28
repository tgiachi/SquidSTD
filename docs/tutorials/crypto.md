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

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Crypto
```

## Next steps

- [Security guide](../articles/guides/security.md)
- [SquidStd.Crypto reference](../articles/crypto.md)
