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

| Type | Purpose |
|------|---------|
| `IVirtualFileSystem` | Path-based filesystem over a pluggable backend. |
| `PhysicalFileSystem` | Maps logical paths onto a real directory tree. |
| `ZipFileSystem` | A single `.zip` archive opened in update mode; `IAsyncDisposable`. |
| `InMemoryFileSystem` | Ephemeral, in-process; handy for tests and as a decorator target. |
| `VfsDirectories` | Named directory layout (`DirectoriesConfig` analogue) over any backend. |
| `RegisterVfsExtensions` | `RegisterVfs(...)` registration. |

## Related

- Tutorial: [Virtual filesystem](https://tgiachi.github.io/squid-std/tutorials/vfs.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
