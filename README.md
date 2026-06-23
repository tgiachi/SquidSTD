<p align="center">
  <img src="assets/icon.png" alt="squid-std" width="128" height="128" />
</p>

<h1 align="center">squid-std</h1>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4.svg" alt=".NET 10" />
  <a href="https://github.com/tgiachi/squid-std/actions/workflows/ci.yml"><img src="https://github.com/tgiachi/squid-std/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <img src="https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/tgiachi/squid-std/gh-pages/badges/tests.json" alt="tests" />
  <img src="https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/tgiachi/squid-std/gh-pages/badges/coverage.json" alt="coverage" />
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

## Overview

**squid-std** is a batteries-included standard library for .NET, distilled from years and years
of building real-world server software. Instead of re-solving the same problems on every project,
it bundles the foundations you reach for again and again behind small, well-defined contracts:

- **Security & hashing** — password hashing and verification (`HashUtils`), AES-GCM secret
  protection and a pluggable secret store (`ISecretProtector` / `ISecretStore`).
- **Configuration** — a YAML-backed config manager with section registration and environment-variable expansion.
- **Serialization** — unified JSON/YAML utilities (`JsonUtils`, `YamlUtils`) and a shared `IDataSerializer` / `IDataDeserializer`.
- **String & platform helpers** — case converters (camel/kebab/snake/pascal/…), network, version, platform and resource utilities.
- **Runtime services** — DI bootstrap, event bus, job system, timer/cron scheduler, metrics and storage.
- **Infrastructure modules** — messaging (in-memory + RabbitMQ), caching (in-memory + Redis),
  database access, networking (TCP/UDP) and Lua scripting.

Everything is modular: take only the packages you need, each behind a clean abstraction with an
in-memory implementation for tests and an external backend for production.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) — only for running the integration tests (Testcontainers spin up RabbitMQ and Redis).

## Quick Start

```bash
dotnet add package SquidStd.Services.Core
dotnet add package SquidStd.Caching
```

```csharp
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Extensions;
using SquidStd.Services.Core.Services.Bootstrap;

// Create the bootstrapper (registers the core services), then opt into the modules you need.
var bootstrap = SquidStdBootstrap.Create()
    .ConfigureServices(container => container.AddInMemoryCache());

await bootstrap.StartAsync();

var cache = bootstrap.Resolve<ICacheService>();
await cache.SetAsync("answer", 42, TimeSpan.FromMinutes(5));
var answer = await cache.GetAsync<int>("answer");

await bootstrap.StopAsync();
```

`SquidStdBootstrap` owns a DryIoc container, wires the core services, and drives the
`StartAsync` / `StopAsync` lifecycle of every registered `ISquidStdService`. Use `RunAsync` to
block until cancellation for long-running hosts.

## Packages

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Core` | Foundational contracts & utilities (config, event bus, jobs, metrics, serialization, YAML/JSON, Serilog sink). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Core/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Core.svg)](https://www.nuget.org/packages/SquidStd.Core/) |
| `SquidStd.Abstractions` | DI registration plumbing (`ISquidStdService`, `RegisterStdService`, `RegisterConfigSection`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Abstractions/) |
| `SquidStd.Services.Core` | Concrete services: config, event bus, jobs, timer/cron scheduler, dispatcher, metrics, health checks, secrets. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Services.Core/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Services.Core.svg)](https://www.nuget.org/packages/SquidStd.Services.Core/) |
| `SquidStd.AspNetCore` | ASP.NET Core host integration for the SquidStd service stack. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.AspNetCore/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.AspNetCore.svg)](https://www.nuget.org/packages/SquidStd.AspNetCore/) |
| `SquidStd.Network` | TCP/UDP servers & clients, sessions, framing/middleware pipeline, span readers/writers. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Network/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Network.svg)](https://www.nuget.org/packages/SquidStd.Network/) |
| `SquidStd.Plugin.Abstractions` | Plugin contracts (`ISquidStdPlugin`, metadata, context). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Plugin.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Plugin.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Plugin.Abstractions/) |
| `SquidStd.Database.Abstractions` | Provider-agnostic data-access contracts (`IDataAccess<T>`, `BaseEntity`, `PagedResultData`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Database.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Database.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Database.Abstractions/) |
| `SquidStd.Database` | FreeSql-backed data access (CRUD/bulk/paging, URI connection strings, ZLinq helpers). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Database/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Database.svg)](https://www.nuget.org/packages/SquidStd.Database/) |
| `SquidStd.Messaging.Abstractions` | Messaging contracts (`IMessageQueue`, `IQueueProvider`, serializer/metrics, listeners). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Messaging.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Messaging.Abstractions/) |
| `SquidStd.Messaging` | In-memory messaging transport (`AddInMemoryMessaging`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Messaging/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.svg)](https://www.nuget.org/packages/SquidStd.Messaging/) |
| `SquidStd.Messaging.RabbitMq` | RabbitMQ messaging transport (`AddRabbitMqMessaging`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Messaging.RabbitMq/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.RabbitMq.svg)](https://www.nuget.org/packages/SquidStd.Messaging.RabbitMq/) |
| `SquidStd.Caching.Abstractions` | Caching contracts (`ICacheService`, `ICacheProvider`, `CacheService` facade, metrics, connection string). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Caching.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Caching.Abstractions/) |
| `SquidStd.Caching` | In-memory cache backend (`AddInMemoryCache`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Caching/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.svg)](https://www.nuget.org/packages/SquidStd.Caching/) |
| `SquidStd.Caching.Redis` | Redis cache backend (`AddRedisCache`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Caching.Redis/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.Redis.svg)](https://www.nuget.org/packages/SquidStd.Caching.Redis/) |
| `SquidStd.Storage.Abstractions` | Storage contracts (`IStorageService`, `IObjectStorageService`, `StorageConfig`, `ListKeysAsync`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Storage.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Storage.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Storage.Abstractions/) |
| `SquidStd.Storage` | Local file storage backend (`AddFileStorage`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Storage/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Storage.svg)](https://www.nuget.org/packages/SquidStd.Storage/) |
| `SquidStd.Storage.S3` | S3/MinIO storage backend (`AddS3Storage`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Storage.S3/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Storage.S3.svg)](https://www.nuget.org/packages/SquidStd.Storage.S3/) |
| `SquidStd.Scripting.Lua` | Lua scripting engine with attribute-based modules and event bridging. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Scripting.Lua/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Scripting.Lua.svg)](https://www.nuget.org/packages/SquidStd.Scripting.Lua/) |
| `SquidStd.Templating` | Scriban templating with a named-template registry and `templates/*.tmpl` auto-load (`AddTemplating`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Templating/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Templating.svg)](https://www.nuget.org/packages/SquidStd.Templating/) |
| `SquidStd.Workers.Abstractions` | Worker/manager shared contracts (`JobRequest`, `WorkerHeartbeat`, `WorkerInfo`, `WorkerChannels`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Workers.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Workers.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Workers.Abstractions/) |
| `SquidStd.Workers` | Worker runtime: consume jobs, dispatch to `IJobHandler`s, publish heartbeats (`AddWorkers`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Workers/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Workers.svg)](https://www.nuget.org/packages/SquidStd.Workers/) |
| `SquidStd.Workers.Manager` | Job enqueue, heartbeat registry, offline sweep, opt-in ASP.NET endpoints (`AddWorkerManager`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Workers.Manager/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Workers.Manager.svg)](https://www.nuget.org/packages/SquidStd.Workers.Manager/) |
| `SquidStd.Templates` | `dotnet new` templates for scaffolding SquidStd projects (host, ASP.NET, worker, manager). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Templates/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Templates.svg)](https://www.nuget.org/packages/SquidStd.Templates/) |

## Architecture

squid-std follows a few consistent principles across every module:

- **KISS** — small, focused types; one class / record / enum per file.
- **Abstractions first** — each capability is split into an `*.Abstractions` package (interfaces,
  DTOs, shared facade) plus one or more provider packages. Consumers depend on the abstraction.
- **In-memory + external provider** — every infrastructure module ships an in-memory provider
  (great for tests and local dev) and a production backend behind the same interface
  (messaging: in-memory / RabbitMQ; caching: in-memory / Redis).
- **DI-driven** — services are registered with DryIoc through `AddXxx(...)` extensions and resolved
  through `SquidStdBootstrap`, which manages the `ISquidStdService` lifecycle.
- **Convention over restructure** — interfaces under `Interfaces`, DTOs under `Data`, enums under
  `Types`, internals under `Internal`. See [`CODE_CONVENTION.md`](CODE_CONVENTION.md).

## Documentation

Full API documentation is published with DocFX to GitHub Pages:
**[tgiachi.github.io/squid-std](https://tgiachi.github.io/squid-std/)**. Each package also ships its
own README (linked in the table above).

## Build

```bash
dotnet build SquidStd.slnx
```

## Test

```bash
dotnet test SquidStd.slnx
```

Integration tests (RabbitMQ, Redis) need Docker running; they use Testcontainers to start
disposable containers automatically.

## Contributing

- Work happens on `develop`; `main` holds released code.
- Use [Conventional Commits](https://www.conventionalcommits.org/) for messages
  (`feat:`, `fix:`, `refactor:`, `docs:`, `build:`, `test:`).
- Follow the project conventions in [`CODE_CONVENTION.md`](CODE_CONVENTION.md).
- Add tests for new behaviour and keep the suite green before opening a PR.

## Versioning & Releases

Releases are automated with [semantic-release](https://semantic-release.gitbook.io/): version
numbers and the changelog are derived from the conventional-commit history, and the NuGet packages
are published on release. Package versions follow [Semantic Versioning](https://semver.org/).

## License

MIT - see [LICENSE](LICENSE).
