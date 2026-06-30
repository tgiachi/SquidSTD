<h1 align="center">SquidStd.Vfs.Database</h1>

An `IVirtualFileSystem` that stores files as rows in a relational database via SquidStd.Database / FreeSql.
Each file is a single `VfsFileEntity` row keyed by path; writes are upserts (last write wins, no concurrent
write safety guarantee).

## Install

```bash
dotnet add package SquidStd.Vfs.Database
```

## Usage

Register `SquidStd.Database` first (it provides `IDataAccess<>`), then add the VFS backend:

```csharp
using DryIoc;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Database.Extensions;

// SquidStd.Database must already be registered on the container.
container.RegisterDatabaseFileSystem();

var fs = container.Resolve<IVirtualFileSystem>();

await fs.WriteAllBytesAsync("config/settings.json", payload);
byte[]? data = await fs.ReadAllBytesAsync("config/settings.json");

await foreach (var entry in fs.ListAsync("config"))
    Console.WriteLine($"{entry.Path} ({entry.Size} bytes)");
```

The `VfsFileEntity` table is created automatically by FreeSql on first access (sync-structure mode).

> **Single-writer assumption** — upsert operations are not serialised across concurrent writers. Use this
> backend in single-process scenarios or where last-write-wins is acceptable.

## Key types

| Type | Purpose |
|---|---|
| `RegisterDatabaseFileSystemExtensions` | `RegisterDatabaseFileSystem()` DryIoc registration. |
| `DatabaseFileSystem` | FreeSql-backed `IVirtualFileSystem`. |
| `VfsFileEntity` | ORM entity: `Path` (PK), `Content` (blob), `UpdatedAt`. |

## Related

- Tutorial: [Virtual filesystem](https://tgiachi.github.io/squid-std/tutorials/vfs.html)
- [`SquidStd.Vfs`](../SquidStd.Vfs/README.md) — core backends and decorators
- [`SquidStd.Database`](../SquidStd.Database/README.md) — required database module

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
