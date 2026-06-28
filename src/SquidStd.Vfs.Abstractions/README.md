<h1 align="center">SquidStd.Vfs.Abstractions</h1>

Virtual filesystem contracts for SquidStd: a path-based file/directory abstraction implemented by physical, in-memory and zip backends.

## Install

```bash
dotnet add package SquidStd.Vfs.Abstractions
```

## Key types

| Type | Purpose |
|------|---------|
| `IVirtualFileSystem` | Path-based filesystem over a pluggable backend (directory, zip, encrypted container): exists, read/write bytes, open read/write streams, delete and list entries. |
| `ILockableFileSystem` | An `IVirtualFileSystem` that stays locked until unlocked with a passphrase (e.g. an encrypted vault), exposing `IsUnlocked`, `Unlock` and `Lock`. |
| `VfsPath` | Static helper that normalizes logical paths to forward-slash, root-relative form and rejects `.`/`..` traversal segments. |
| `VfsEntry` | Record describing a listed entry: its logical path, byte size and last-modified UTC timestamp. |

## Related

- Tutorial: [Virtual filesystem](https://tgiachi.github.io/squid-std/tutorials/vfs.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
