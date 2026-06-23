<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Storage</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Storage/"><img src="https://img.shields.io/nuget/v/SquidStd.Storage.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Storage.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/storage.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Local file storage for SquidStd. Provides a filesystem-backed `IStorageService` (atomic writes,
path-safe keys) and a YAML-backed `IObjectStorageService` that layers typed objects on top of it —
registered with a single `AddFileStorage()` call. Storage is opt-in: it is not registered by `RegisterCoreServices`.

## Install

```bash
dotnet add package SquidStd.Storage
```

## Features

- One-line registration: `container.AddFileStorage()` (file `IStorageService` + YAML `IObjectStorageService`).
- Atomic saves (temp file + move) and keys constrained to the storage root.
- `YamlObjectStorageService` decorates `IStorageService`, serializing typed objects to YAML.
- `ListKeysAsync(prefix?)` enumerates stored keys (`/`-separated), excluding in-flight temp files.

## Usage

```csharp
using DryIoc;
using SquidStd.Storage.Abstractions.Data.Config;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.Extensions;

var container = new Container();
container.AddFileStorage(new StorageConfig { RootDirectory = "data" });

var storage = container.Resolve<IStorageService>();
await storage.SaveAsync("profiles/main.bin", new byte[] { 1, 2, 3 });
```

## Key types

| Type | Purpose |
|------|---------|
| `StorageRegistrationExtensions` | `AddFileStorage(...)` registration. |
| `FileStorageService` | Filesystem-backed `IStorageService`. |
| `YamlObjectStorageService` | YAML-backed `IObjectStorageService` over a blob store. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
