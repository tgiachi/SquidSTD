# Virtual filesystem

Read and write files through an abstract virtual filesystem, then layer an encrypted vault on top.

## What you'll build

A host that resolves `IVirtualFileSystem` (`SquidStd.Vfs`) backed by an in-memory store, plus an
encrypted-vault round-trip using `CryptoFileSystem` (`SquidStd.Crypto.Vfs`).

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Vfs` (and `SquidStd.Crypto.Vfs` for the encrypted vault)

## Steps

### 1. Register a virtual filesystem

`RegisterVfs` wires `IVirtualFileSystem`; the factory chooses the backend — here a plain
in-memory filesystem.

[!code-csharp[](../../samples/SquidStd.Samples.Vfs/Program.cs#step-1)]

### 2. Write and read a file

The same `IVirtualFileSystem` API works regardless of the backend. `ReadAllBytesAsync` returns
`null` only when the path is absent.

[!code-csharp[](../../samples/SquidStd.Samples.Vfs/Program.cs#step-2)]

### 3. Encrypted vault round-trip

`CryptoFileSystem` encrypts every entry over a backend filesystem. The lifecycle is
unlock → write → lock; locking zeroes the key and flushes the encrypted index into the backend,
so re-opening the same backend with the passphrase decrypts the data at rest.

This sample drives `CryptoFileSystem` over an **in-memory** backend to demonstrate the full
lifecycle. The DI helper `RegisterCryptoVault` wires a vault over a single on-disk zip file, but
that `ZipFileSystem` backend currently cannot be locked/persisted (a known limitation — its
`List` reads `ZipArchiveEntry.Length`, which .NET marks unavailable in `ZipArchiveMode.Update`).
On-disk vault persistence is therefore not yet supported.

[!code-csharp[](../../samples/SquidStd.Samples.Vfs/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Vfs
```

## Next steps

- [SquidStd.Vfs reference](../articles/vfs.md)
