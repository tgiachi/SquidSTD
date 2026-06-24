# Caching (in-memory + Redis)

Cache typed values with a TTL, then swap the in-memory backend for Redis without changing your code.

## What you'll build

A host that resolves `ICacheService` (`SquidStd.Caching.Abstractions`) backed by the in-memory provider
(`SquidStd.Caching`), and the one-line change to use Redis (`SquidStd.Caching.Redis`) instead.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Caching` (and `SquidStd.Caching.Redis` for the Redis backend)
- For Redis: `docker run -p 6379:6379 redis`

## Steps

### 1. Register the in-memory cache

[!code-csharp[](../../samples/SquidStd.Samples.Caching/Program.cs#step-1)]

### 2. Set and get typed values

`ICacheService` stores typed values with an optional TTL; `GetAsync<T>` returns `null` on a miss.

[!code-csharp[](../../samples/SquidStd.Samples.Caching/Program.cs#step-2)]

### 3. Switch to Redis

Replace the registration with the Redis backend — the `ICacheService` usage is identical:

```csharp
container.AddRedisCache("redis://localhost:6379");
```

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Caching
```

Prints `hello`.

## How it works

`ICacheService` is the typed facade over an `ICacheProvider`; the provider is the swappable backend (in-memory or
Redis). Values are serialized with the shared `IDataSerializer`, so the same code works on either backend.

## See also

- [SquidStd.Caching reference](../articles/caching.md)
- [SquidStd.Caching.Redis reference](../articles/caching-redis.md)
