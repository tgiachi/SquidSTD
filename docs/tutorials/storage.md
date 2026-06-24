# Object storage (file system + S3)

Save and load typed objects by key on the local file system, then swap to S3 without changing your code.

## What you'll build

A host that resolves `IObjectStorageService` (`SquidStd.Storage.Abstractions`) backed by the file-system provider
(`SquidStd.Storage`), and the one-line change to use S3 instead.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Storage` (and `SquidStd.Storage.S3` for the S3 backend)
- For S3: an S3-compatible endpoint and bucket (e.g. AWS S3 or MinIO)

## Steps

### 1. Register file storage

[!code-csharp[](../../samples/SquidStd.Samples.Storage/Program.cs#step-1)]

### 2. Save and load a typed object

`IObjectStorageService` stores typed objects by logical key; `LoadAsync<T>` returns `null` on a miss.

[!code-csharp[](../../samples/SquidStd.Samples.Storage/Program.cs#step-2)]

### 3. Switch to S3

Replace the registration with the S3 backend — the `IObjectStorageService` usage is identical:

```csharp
container.AddS3Storage(/* endpoint, bucket and credentials */);
```

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Storage
```

Prints `squid <squid@stormwind.it>`.

## How it works

`IObjectStorageService` is the typed facade over a swappable storage backend (local file system or S3). Values are
serialized with the shared `IDataSerializer`, so the same `SaveAsync`/`LoadAsync` code works on either backend.

## See also

- [SquidStd.Storage reference](../articles/storage.md)
- [SquidStd.Storage.S3 reference](../articles/storage-s3.md)
