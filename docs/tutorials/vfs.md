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
unlock → write → dispose; disposing locks the vault (zeroing the key and flushing the encrypted
index) and disposes the backend, so a `ZipFileSystem` backend flushes its archive to disk.
Re-opening a fresh instance over the same file with the passphrase decrypts the data at rest.

This sample backs the vault with a single on-disk zip file via `ZipFileSystem`, writes a secret,
disposes the vault, then re-opens a brand-new instance over the same file to prove on-disk
persistence. The DI helper `RegisterCryptoVault` wires exactly this — a vault over a single-file
zip — as a lockable singleton.

[!code-csharp[](../../samples/SquidStd.Samples.Vfs/Program.cs#step-3)]

## Alternative backends

### S3-compatible storage

Swap the in-memory backend for an S3-compatible store (AWS S3, MinIO, Cloudflare R2, Backblaze B2)
by installing `SquidStd.Vfs.S3` and replacing the `RegisterVfs` call:

```bash
dotnet add package SquidStd.Vfs.S3
```

```csharp
using SquidStd.Vfs.S3.Extensions;

container.RegisterS3FileSystem(o =>
{
    o.Bucket         = "app-data";
    o.Aws.ServiceUrl = "https://s3.amazonaws.com";  // or MinIO/R2/B2 endpoint
    o.Aws.AccessKey  = "...";
    o.Aws.SecretKey  = "...";
});

// IVirtualFileSystem is now backed by S3 — the rest of your code is unchanged.
var fs = container.Resolve<IVirtualFileSystem>();
await fs.WriteAllBytesAsync("reports/2026.json", payload);
```

For native AWS with the default credential chain, omit `AccessKey`/`SecretKey` and set only
`o.Aws.Region`.

### Database-backed storage

Store files as rows in a relational database (SQLite, MySQL, PostgreSQL, …) with
`SquidStd.Vfs.Database`. Register `SquidStd.Database` first, then add the VFS backend:

```bash
dotnet add package SquidStd.Vfs.Database
```

```csharp
using SquidStd.Vfs.Database.Extensions;

// SquidStd.Database must already be registered on the container.
container.RegisterDatabaseFileSystem();

var fs = container.Resolve<IVirtualFileSystem>();
await fs.WriteAllBytesAsync("config/settings.json", payload);
```

FreeSql creates the `VfsFileEntity` table automatically on first access. This backend is
well-suited to single-process scenarios or where last-write-wins is acceptable.

## Composable decorators

Decorators wrap any `IVirtualFileSystem` to add behaviour without touching the backend. Construct
them directly and pass the result to `RegisterVfs`:

### Offline-resilient reads with `CachingFileSystem`

Wrap a remote backend (S3, database) with a local cache to keep reads working when the remote is
temporarily unreachable:

```csharp
using SquidStd.Vfs.Services;
using SquidStd.Vfs.S3.Services;

var s3 = new S3FileSystem(s3Options);

container.RegisterVfs(_ => new CachingFileSystem(
    remote: s3,
    cache:  new PhysicalFileSystem("/var/cache/app")));
```

Reads prefer the remote and refresh the cache on success; on a transport failure they fall back to
the stale cached copy. Writes are write-through (remote then cache) and fail when the remote is
unreachable.

### Other decorators

```csharp
// Reject all writes — safe read-only access to a shared backend.
container.RegisterVfs(_ => new ReadOnlyFileSystem(new PhysicalFileSystem("/shared/data")));

// Chroot to a subdirectory — all paths are resolved relative to the prefix.
container.RegisterVfs(_ => new ScopedFileSystem(new PhysicalFileSystem("/var/lib/app"), "tenant-1"));

// Overlay — reads overlay-first then fall back to base; writes go to the overlay only.
container.RegisterVfs(_ => new OverlayFileSystem(
    baseFileSystem: new PhysicalFileSystem("/defaults"),
    overlay:        new InMemoryFileSystem()));
```

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Vfs
```

## Next steps

- [SquidStd.Vfs reference](../articles/vfs.md)
- [SquidStd.Vfs.S3 reference](../articles/vfs-s3.md)
- [SquidStd.Vfs.Database reference](../articles/vfs-database.md)
