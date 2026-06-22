<p align="center">
  <img src="assets/icon.png" alt="squid-std" width="128" height="128" />
</p>

<h1 align="center">squid-std</h1>

<p align="center">
  <a href="https://github.com/tgiachi/SquidStd/actions/workflows/ci.yml"><img src="https://github.com/tgiachi/SquidStd/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <img src="https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/tgiachi/SquidStd/gh-pages/badges/tests.json" alt="tests" />
  <img src="https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/tgiachi/SquidStd/gh-pages/badges/coverage.json" alt="coverage" />
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

## Overview

Short description goes here.

## Packages

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Core` | Foundational contracts & utilities (config, event bus, jobs, metrics, storage, YAML/JSON, Serilog sink). | [readme](src/SquidStd.Core/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Core.svg)](https://www.nuget.org/packages/SquidStd.Core/) |
| `SquidStd.Abstractions` | DI registration plumbing (`ISquidStdService`, `RegisterStdService`, `RegisterConfigSection`). | [readme](src/SquidStd.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Abstractions/) |
| `SquidStd.Services.Core` | Concrete services: config, event bus, jobs, timer/cron scheduler, dispatcher, metrics, storage. | [readme](src/SquidStd.Services.Core/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Services.Core.svg)](https://www.nuget.org/packages/SquidStd.Services.Core/) |
| `SquidStd.AspNetCore` | ASP.NET Core host integration for the SquidStd service stack. | [readme](src/SquidStd.AspNetCore/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.AspNetCore.svg)](https://www.nuget.org/packages/SquidStd.AspNetCore/) |
| `SquidStd.Network` | TCP/UDP servers & clients, sessions, framing/middleware pipeline, span readers/writers. | [readme](src/SquidStd.Network/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Network.svg)](https://www.nuget.org/packages/SquidStd.Network/) |
| `SquidStd.Plugin.Abstractions` | Plugin contracts (`ISquidStdPlugin`, metadata, context). | [readme](src/SquidStd.Plugin.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Plugin.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Plugin.Abstractions/) |
| `SquidStd.Database.Abstractions` | Provider-agnostic data-access contracts (`IDataAccess<T>`, `BaseEntity`, `PagedResultData`). | [readme](src/SquidStd.Database.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Database.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Database.Abstractions/) |
| `SquidStd.Database` | FreeSql-backed data access (CRUD/bulk/paging, URI connection strings, ZLinq helpers). | [readme](src/SquidStd.Database/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Database.svg)](https://www.nuget.org/packages/SquidStd.Database/) |
| `SquidStd.Messaging.Abstractions` | Messaging contracts (`IMessageQueue`, `IQueueProvider`, serializer/metrics, listeners). | [readme](src/SquidStd.Messaging.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Messaging.Abstractions/) |
| `SquidStd.Messaging` | In-memory messaging transport (`AddInMemoryMessaging`). | [readme](src/SquidStd.Messaging/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.svg)](https://www.nuget.org/packages/SquidStd.Messaging/) |
| `SquidStd.Messaging.RabbitMq` | RabbitMQ messaging transport (`AddRabbitMqMessaging`). | [readme](src/SquidStd.Messaging.RabbitMq/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.RabbitMq.svg)](https://www.nuget.org/packages/SquidStd.Messaging.RabbitMq/) |
| `SquidStd.Caching.Abstractions` | Caching contracts (`ICacheService`, `ICacheProvider`, `CacheService` facade, metrics, connection string). | [readme](src/SquidStd.Caching.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Caching.Abstractions/) |
| `SquidStd.Caching` | In-memory cache backend (`AddInMemoryCache`). | [readme](src/SquidStd.Caching/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.svg)](https://www.nuget.org/packages/SquidStd.Caching/) |
| `SquidStd.Caching.Redis` | Redis cache backend (`AddRedisCache`). | [readme](src/SquidStd.Caching.Redis/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.Redis.svg)](https://www.nuget.org/packages/SquidStd.Caching.Redis/) |
| `SquidStd.Scripting.Lua` | Lua scripting engine with attribute-based modules and event bridging. | [readme](src/SquidStd.Scripting.Lua/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Scripting.Lua.svg)](https://www.nuget.org/packages/SquidStd.Scripting.Lua/) |

## Build

```bash
dotnet build SquidStd.slnx
```

## Test

```bash
dotnet test SquidStd.slnx
```

## License

MIT - see [LICENSE](LICENSE).
