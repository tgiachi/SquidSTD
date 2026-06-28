<h1 align="center">SquidStd.Crypto</h1>

OpenPGP key management and operations for SquidStd, built on [PgpCore](https://github.com/mattosaurus/PgpCore)
(a maintained MIT wrapper over BouncyCastle). Provides key generation, encrypt/decrypt, sign/verify, and the
combined encrypt+sign / decrypt+verify flows over a stateful, indexed **keyring** with a pluggable persistence
backend. `SquidStd.Core` stays dependency-free; this module is the home for higher-level crypto features, with
PGP namespaced under `SquidStd.Crypto.Pgp` so future areas can coexist.

## Install

```bash
dotnet add package SquidStd.Crypto
```

## Usage

```csharp
using DryIoc;
using SquidStd.Crypto.Pgp.Extensions;
using SquidStd.Crypto.Pgp.Interfaces;
using SquidStd.Crypto.Pgp.Services;

// Register the keyring, service, and a key store (all singletons).
container.RegisterPgp(_ => new FilePgpKeyStore("/var/lib/app/pgp"));
// or encrypted at rest with the app key:
// container.RegisterPgp(r => new AesGcmPgpKeyStore(r.Resolve<ISecretProtector>(), "/var/lib/app/pgp.bin"));

var keyring = container.Resolve<IPgpKeyring>();
var pgp = container.Resolve<IPgpService>();

// Generate a key and import it into the keyring.
var alice = pgp.GenerateKey("alice@example.com", "passphrase");
keyring.Import(alice.PrivateArmored!);
keyring.Import(bobPublicArmored); // a correspondent's public key

// Encrypt for a recipient, decrypt with the held secret key.
string armored = await pgp.EncryptForAsync("bob@example.com", payloadBytes);
byte[] plaintext = await pgp.DecryptAsync(armoredFromBob, "passphrase");

// Sign and verify (signed message — the data is embedded in the armored block).
string signed = await pgp.SignAsync(payloadBytes, "alice@example.com", "passphrase");
var verification = await pgp.VerifyAsync(signed);   // verification.IsValid, verification.Data

// Combined: encrypt + sign, then decrypt + verify.
string sealed = await pgp.EncryptAndSignForAsync("bob@example.com", payloadBytes, "alice@example.com", "passphrase");
var result = await pgp.DecryptAndVerifyAsync(sealedFromBob, "passphrase"); // result.Data, result.IsSigned, result.IsValid

// Persist / restore the keyring.
await keyring.SaveAsync(container.Resolve<IPgpKeyStore>());
await keyring.LoadAsync(container.Resolve<IPgpKeyStore>());
```

## Key stores

- **`FilePgpKeyStore(directory)`** — one armored `.asc` per key (public, plus secret when held). gpg-interoperable.
- **`AesGcmPgpKeyStore(ISecretProtector, path)`** — the whole keyring serialized to a single file, encrypted at
  rest with the application key via `SquidStd`'s `ISecretProtector`.

## Notes

- **Signatures are signed messages**, not detached signatures: `SignAsync` embeds the data in the armored block
  and `VerifyAsync` recovers it. Verification is **pass/fail** — PgpCore does not expose the signer's key id or
  identity, so the results carry no signer attribution.
- `DecryptAndVerifyAsync` never throws on a bad/absent signature when the ciphertext itself is valid: it always
  recovers `Data` and reports `IsSigned` / `IsValid`.
- The streaming `DecryptAsync(Stream, Stream, …)` buffers its input internally so the recipient key id can be
  read before decrypting; the encrypt and sign stream paths flow straight through.
- Passphrases are supplied per operation and never persisted (only passphrase-protected secret blocks are
  stored).

## Crypto vault (encrypted virtual filesystem)

`CryptoFileSystem` is an `ILockableFileSystem` that **decorates any `IVirtualFileSystem`**
(`SquidStd.Vfs`), encrypting file content and names. `Crypto(Zip("vault.dat"))` is a single-file encrypted
vault you unlock with a passphrase, write to, and lock again.

```csharp
using DryIoc;
using SquidStd.Crypto.Vfs.Extensions;
using SquidStd.Vfs.Abstractions.Interfaces;

// Single-file vault (crypto over a zip backend), registered as a singleton.
container.RegisterCryptoVault("/var/lib/app/vault.dat");

var vault = container.Resolve<ILockableFileSystem>();
vault.Unlock("my passphrase");          // derives the key (Argon2id)
await vault.WriteAllBytesAsync("docs/cv.pdf", bytes);
byte[]? back = await vault.ReadAllBytesAsync("docs/cv.pdf");
vault.Lock();                            // flushes, prunes, zeroes the key
```

Compose other backends directly:

```csharp
using SquidStd.Crypto.Vfs.Services;
using SquidStd.Vfs.Services;

var folderVault = new CryptoFileSystem(new PhysicalFileSystem("/secure/dir"));
```

### Notes

- **Unlock with a passphrase**: a 256-bit key is derived with **Argon2id** (salt + cost params live in the
  cleartext header). Per-purpose subkeys are derived with HKDF-SHA256. The passphrase is never persisted.
- **Per-entry encryption**: each file is stored as **chunked AES-GCM** (64 KiB chunks), so large files stream
  with bounded memory and tampering/truncation is detected.
- **Encrypted name index**: logical paths and structure live in an encrypted index; backing entries use opaque
  ids, so a locked vault leaks neither file names nor layout.
- **Read-write, lockable**: add/update/delete any time while unlocked; `Lock()`/`Dispose()` flush the encrypted
  index, prune orphaned blobs, and zero the key with `CryptographicOperations.ZeroMemory`.
- A wrong passphrase fails the index authentication tag → `CryptographicException`; operations on a locked
  vault throw `InvalidOperationException`.
