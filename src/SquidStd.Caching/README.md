<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Caching</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Caching/"><img src="https://img.shields.io/nuget/v/SquidStd.Caching.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Caching.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/caching.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

In-memory backend for SquidStd.Caching. Provides an `IMemoryCache`-backed `ICacheProvider` with
absolute TTL and eviction, wired to the shared typed `ICacheService` facade — registered with a
single `AddInMemoryCache()` call. Ideal for single-process apps, tests, and local dev.

## Install

```bash
dotnet add package SquidStd.Caching
```

## Features

- One-line registration: `container.AddInMemoryCache()` (provider, facade, serializer, metrics).
- `IMemoryCache`-backed storage with absolute per-entry TTL and built-in eviction.
- Reuses the shared `CacheService` facade (key prefix, default TTL, cache-aside).
- Built-in hit/miss metrics via `CacheMetricsProvider`.
- Configure via `CacheOptions` or a `memory://` connection string (`?defaultTtlSeconds=`, `?keyPrefix=`).

## Usage

```csharp
using DryIoc;
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Extensions;

var container = new Container();
container.AddInMemoryCache("memory://localhost?defaultTtlSeconds=300&keyPrefix=app:");

var cache = container.Resolve<ICacheService>();
await cache.SetAsync("user:1", new { Name = "squid" });
var user = await cache.GetAsync<object>("user:1");
```

## Key types

| Type | Purpose |
|------|---------|
| `CacheRegistrationExtensions` | `AddInMemoryCache(...)` registration. |
| `InMemoryCacheProvider` | `IMemoryCache`-backed `ICacheProvider`. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
