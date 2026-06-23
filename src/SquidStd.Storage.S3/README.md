<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Storage.S3</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Storage.S3/"><img src="https://img.shields.io/nuget/v/SquidStd.Storage.S3.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Storage.S3.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/storage-s3.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

S3-compatible storage for SquidStd, backed by the MinIO .NET SDK. Implements `IStorageService` against
AWS S3, MinIO, or any S3-compatible endpoint, so the same storage API reads and writes object storage.
The bucket is created lazily on first use. Registered with a single `AddS3Storage(...)` call.

## Install

```bash
dotnet add package SquidStd.Storage.S3
```

## Features

- One-line registration: `container.AddS3Storage(options)`.
- `IStorageService` over `IMinioClient` (`SaveAsync` / `LoadAsync` / `DeleteAsync` / `ExistsAsync` / `ListKeysAsync`).
- Lazy bucket creation; `ListKeysAsync(prefix?)` streams object keys via the S3 list API.
- Works with AWS S3 and MinIO via `S3StorageOptions` (endpoint, credentials, bucket, TLS, region).

## Usage

```csharp
using DryIoc;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.S3.Data.Config;
using SquidStd.Storage.S3.Extensions;

var container = new Container();
container.AddS3Storage(new S3StorageOptions
{
    Endpoint = "localhost:9000",
    AccessKey = "minioadmin",
    SecretKey = "minioadmin",
    Bucket = "app-data"
});

var storage = container.Resolve<IStorageService>();
await storage.SaveAsync("reports/2026.json", "{}"u8.ToArray());
```

## Key types

| Type | Purpose |
|------|---------|
| `S3StorageRegistrationExtensions` | `AddS3Storage(...)` registration. |
| `S3StorageService` | MinIO-backed `IStorageService`. |
| `S3StorageOptions` | Endpoint, credentials, bucket, TLS, region. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
