<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Storage.Abstractions</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Storage.Abstractions/"><img src="https://img.shields.io/nuget/v/SquidStd.Storage.Abstractions.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Storage.Abstractions.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/storage-abstractions.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Backend-agnostic storage contracts for SquidStd. It defines the binary blob store (`IStorageService`),
the typed object store (`IObjectStorageService`), key enumeration (`ListKeysAsync`), and `StorageConfig`.
Pick a backend implementation (local file or S3/MinIO) from a companion package.

## Install

```bash
dotnet add package SquidStd.Storage.Abstractions
```

## Features

- `IStorageService` — binary blobs by key: `SaveAsync` / `LoadAsync` / `DeleteAsync` / `ExistsAsync` / `ListKeysAsync`.
- `IObjectStorageService` — typed objects by key (serialized by the provider) with the same surface plus `ListKeysAsync`.
- `ListKeysAsync(prefix?)` — streams stored keys as `IAsyncEnumerable<string>`, optionally filtered by prefix.
- `StorageConfig` — root-directory configuration for file-backed storage.

## Usage

```csharp
using SquidStd.Storage.Abstractions.Interfaces;

// Resolve IStorageService from a backend package (file or S3).
public async Task DumpKeysAsync(IStorageService storage)
{
    await foreach (var key in storage.ListKeysAsync("profiles/"))
    {
        Console.WriteLine(key);
    }
}
```

## Key types

| Type | Purpose |
|------|---------|
| `IStorageService` | Binary blob store (save/load/delete/exists/list). |
| `IObjectStorageService` | Typed object store over a blob backend. |
| `StorageConfig` | Root directory for file storage. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
