<h1 align="center">SquidStd.Vfs.S3</h1>

An `IVirtualFileSystem` over S3-compatible object storage — AWS S3, MinIO, Cloudflare R2, Backblaze B2 — via
the MinIO .NET SDK. Each logical path maps directly to an object key inside a single bucket. The bucket is
created lazily on first use.

## Install

```bash
dotnet add package SquidStd.Vfs.S3
```

## Usage

```csharp
using DryIoc;
using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.S3.Extensions;

container.RegisterS3FileSystem(o =>
{
    o.Bucket          = "app-data";
    o.Aws.ServiceUrl  = "https://s3.amazonaws.com";   // or MinIO/R2/B2 endpoint
    o.Aws.AccessKey   = "...";
    o.Aws.SecretKey   = "...";
});

var fs = container.Resolve<IVirtualFileSystem>();

await fs.WriteAllBytesAsync("reports/2026.json", payload);
byte[]? data = await fs.ReadAllBytesAsync("reports/2026.json");

await foreach (var entry in fs.ListAsync("reports"))
    Console.WriteLine($"{entry.Path} ({entry.Size} bytes)");
```

For native AWS with the default credential chain, omit `AccessKey`/`SecretKey` and set only the region via
`o.Aws.Region`.

## Key types

| Type | Purpose |
|---|---|
| `RegisterS3FileSystemExtensions` | `RegisterS3FileSystem(...)` DryIoc registration. |
| `S3FileSystem` | MinIO-backed `IVirtualFileSystem` with lazy bucket creation. |
| `S3FileSystemOptions` | `Bucket` + `Aws` (`ServiceUrl`, `AccessKey`, `SecretKey`, `Region`). |

## Related

- Tutorial: [Virtual filesystem](https://tgiachi.github.io/squid-std/tutorials/vfs.html)
- [`SquidStd.Vfs`](../SquidStd.Vfs/README.md) — core backends and decorators

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
