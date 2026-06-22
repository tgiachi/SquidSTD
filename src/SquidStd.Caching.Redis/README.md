<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Caching.Redis</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Caching.Redis/"><img src="https://img.shields.io/nuget/v/SquidStd.Caching.Redis.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Caching.Redis.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/caching-redis.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Redis backend for SquidStd.Caching. Implements `ICacheProvider` on top of StackExchange.Redis,
so the same `ICacheService` API reads and writes a real Redis server with native key expiry.
Registered with a single `AddRedisCache(...)` call.

## Install

```bash
dotnet add package SquidStd.Caching.Redis
```

## Features

- One-line registration: `container.AddRedisCache(connectionString)` or with `RedisCacheOptions`.
- Redis-backed `ICacheProvider` reusing the shared `ICacheService` facade and serializer.
- Native TTL via Redis key expiry (`SET ... EX`).
- Connection via a `redis://` connection string or an explicit StackExchange.Redis configuration string.
- Built-in hit/miss metrics via `CacheMetricsProvider`.

## Usage

```csharp
using DryIoc;
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Redis.Extensions;

var container = new Container();
container.AddRedisCache("redis://localhost:6379?defaultTtlSeconds=300&keyPrefix=app:");

var cache = container.Resolve<ICacheService>();
await cache.SetAsync("user:1", new { Name = "squid" }, TimeSpan.FromMinutes(10));
var user = await cache.GetAsync<object>("user:1");
```

## Key types

| Type | Purpose |
|------|---------|
| `RedisCacheRegistrationExtensions` | `AddRedisCache(...)` registration. |
| `RedisCacheProvider` | StackExchange.Redis-backed `ICacheProvider`. |
| `RedisCacheOptions` | StackExchange.Redis connection configuration. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
