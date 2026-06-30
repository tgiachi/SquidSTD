<h1 align="center">SquidStd.Vfs</h1>

A path-based virtual filesystem abstraction for SquidStd with interchangeable backends. One interface
(`IVirtualFileSystem`) is implemented by real directories, a single zip archive, and an in-memory store — and
decorated by an encrypted vault in `SquidStd.Crypto`. `VfsDirectories` is a VFS-backed analogue of
`DirectoriesConfig`, so named directory layouts work over any backend (a zip or an encrypted container can
stand in for real folders).

## Install

```bash
dotnet add package SquidStd.Vfs
```

## Usage

```csharp
using DryIoc;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Extensions;
using SquidStd.Vfs.Services;

// Register a backend as IVirtualFileSystem (singleton).
container.RegisterVfs(_ => new PhysicalFileSystem("/var/lib/app/data"));
// or: new ZipFileSystem("/var/lib/app/data.zip")
// or: new InMemoryFileSystem()

var fs = container.Resolve<IVirtualFileSystem>();

await fs.WriteAllBytesAsync("docs/cv.pdf", bytes);
byte[]? data = await fs.ReadAllBytesAsync("docs/cv.pdf");

await foreach (var entry in fs.ListAsync("docs"))
{
    Console.WriteLine($"{entry.Path} ({entry.Size} bytes)");
}

await fs.DeleteAsync("docs/cv.pdf");
```

Named directories over any backend:

```csharp
var dirs = new VfsDirectories(fs, ["data", "logs"]);
var target = dirs.Combine("data", "report.csv");   // "data/report.csv"
await fs.WriteAllBytesAsync(target, csvBytes);
```

Logical paths are normalized (forward slashes, root-relative) and reject `..` traversal via
`SquidStd.Vfs.Abstractions.VfsPath`.

## Key types

| Type                    | Purpose                                                                 |
|-------------------------|-------------------------------------------------------------------------|
| `IVirtualFileSystem`    | Path-based filesystem over a pluggable backend.                         |
| `PhysicalFileSystem`    | Maps logical paths onto a real directory tree.                          |
| `ZipFileSystem`         | A single `.zip` archive opened in update mode; `IAsyncDisposable`.      |
| `InMemoryFileSystem`    | Ephemeral, in-process; handy for tests and as a decorator target.       |
| `VfsDirectories`        | Named directory layout (`DirectoriesConfig` analogue) over any backend. |
| `RegisterVfsExtensions` | `RegisterVfs(...)` registration.                                        |

## Decorators

Decorators wrap any `IVirtualFileSystem` to add behaviour without touching the backend. Stack them in any order.

| Decorator | Description |
|---|---|
| `ReadOnlyFileSystem(inner)` | Delegates all reads to `inner`; rejects every mutation with `InvalidOperationException`. |
| `ScopedFileSystem(inner, prefix)` | Roots `inner` at a path prefix (chroot-like). All paths are resolved relative to the scope; list results are returned relative to it too. |
| `OverlayFileSystem(base, overlay)` | Reads overlay-first then falls back to base. Writes and deletes go to the overlay only. List returns the union of both; overlay entries shadow base entries with the same path. |
| `CachingFileSystem(remote, cache)` | Read-through cache: reads prefer the remote and refresh the cache copy on success; on a transport failure they fall back to the (possibly stale) cache. Writes are write-through (remote then cache) and fail when the remote is unreachable. |

Composition example — S3 with a local disk cache for resilience to an unstable connection:

```csharp
// S3 with a local disk cache for resilience to an unstable connection.
var fs = new CachingFileSystem(
    remote: s3FileSystem,
    cache:  new PhysicalFileSystem("/var/cache/app"));
```

Decorators are not registered via DI helpers; construct them explicitly and pass the result to `RegisterVfs(...)`:

```csharp
container.RegisterVfs(_ => new ReadOnlyFileSystem(new PhysicalFileSystem("/var/lib/app/data")));
```

## Related

- Tutorial: [Virtual filesystem](https://tgiachi.github.io/squid-std/tutorials/vfs.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
