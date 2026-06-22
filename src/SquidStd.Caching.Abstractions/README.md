<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Caching.Abstractions</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Caching.Abstractions/"><img src="https://img.shields.io/nuget/v/SquidStd.Caching.Abstractions.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Caching.Abstractions.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/caching-abstractions.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Backend-agnostic caching contracts for SquidStd. It defines the typed cache-aside facade
(`ICacheService`), the low-level byte provider/metrics contracts, and the shared `CacheService`
facade that applies key-prefixing, default TTL and cache-aside once over any provider. Pick a
backend implementation (in-memory or Redis) from a companion package.

## Install

```bash
dotnet add package SquidStd.Caching.Abstractions
```

## Features

- `ICacheService` — typed `GetAsync<T>` / `SetAsync<T>` / `RemoveAsync` / `ExistsAsync` / `GetOrSetAsync<T>` facade.
- `ICacheProvider` — the raw byte-level backend contract implemented per provider.
- `CacheService` — shared facade that serializes values, applies the key prefix and default TTL, and implements cache-aside.
- `ICacheMetrics` (+ `CacheMetricsProvider`, `NoOpCacheMetrics`) — hit/miss/set/remove metrics.
- `CacheOptions` and `CacheConnectionString` — configuration and connection parsing.

## Usage

```csharp
using SquidStd.Caching.Abstractions.Interfaces;

// Resolve ICacheService from a backend package (in-memory or Redis).
public Task<int> GetOrComputeAsync(ICacheService cache)
    => cache.GetOrSetAsync("answer", _ => Task.FromResult(42), TimeSpan.FromMinutes(5));
```

## Key types

| Type | Purpose |
|------|---------|
| `ICacheService` | Typed cache-aside facade. |
| `ICacheProvider` | Byte-level backend contract (per provider). |
| `CacheService` | Shared facade: serialization, key prefix, default TTL, cache-aside. |
| `ICacheMetrics` | Hit/miss/set/remove metrics sink. |
| `CacheOptions` | Default TTL and key prefix. |
| `CacheConnectionString` | `scheme://host[?params]` parsing into `CacheOptions`. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
