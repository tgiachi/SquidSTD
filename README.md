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

## Contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Project templates](#project-templates)
- [Packages](#packages)
- [Related projects](#related-projects)
- [Architecture](#architecture)
- [Documentation](#documentation)
- [Build](#build)
- [Test](#test)
- [Contributing](#contributing)
- [Versioning & Releases](#versioning--releases)
- [Built on](#built-on)
- [License](#license)

## Overview

**squid-std** is a batteries-included standard library for .NET, distilled from years and years
of building real-world server software. Instead of re-solving the same problems on every project,
it bundles the foundations you reach for again and again behind small, well-defined contracts:

- **Security & crypto** — password hashing (`HashUtils`), AES-GCM secret protection and a pluggable
  secret store (`ISecretProtector` / `ISecretStore`), an OpenPGP keyring (`SquidStd.Crypto`), and
  AWS KMS / Secrets Manager adapters.
- **Configuration & serialization** — a YAML-backed config manager with section registration and
  environment-variable expansion; unified JSON/YAML utilities and a shared `IDataSerializer`.
- **Runtime services** — DI bootstrap, event bus, command dispatcher, actors (ordered per-entity
  mailboxes), job system, timer/cron scheduler, metrics and health checks.
- **Messaging & caching** — in-memory, RabbitMQ and AWS SQS/SNS transports; in-memory and Redis caches.
- **Data & storage** — FreeSql data access, binary persistence (snapshot + WAL), local / S3 / MinIO
  object storage, and a virtual filesystem (physical / zip / in-memory + an encrypted vault, plus
  S3-compatible and database-backed backends and composable decorators).
- **Search, mail & workers** — Elasticsearch indexing with a constrained LINQ provider, IMAP/POP3
  mail polling and an outbound mail queue, and a worker / manager runtime.
- **Networking, scripting & observability** — TCP/UDP servers with a framing/middleware pipeline,
  Lua scripting and Scriban templating, and OpenTelemetry tracing + metrics export.
- **Terminal UI** — MVVM for terminal apps via `SquidStd.Tui`: observable ViewModels, a fluent
  binder (+ `AutoBind` and a declarative DSL), ViewModel-first navigation, DryIoc wiring.

Everything is modular: take only the packages you need, each behind a clean abstraction with an
in-memory implementation for tests and an external backend for production.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) — only for the integration tests (Testcontainers spin up RabbitMQ, Redis, Elasticsearch, MinIO and LocalStack on demand).

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

### A fuller example: event bus + a provider

```csharp
using SquidStd.Core.Interfaces.Events;
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Extensions;
using SquidStd.Services.Core.Services.Bootstrap;

public sealed record OrderPlaced(string OrderId) : IEvent;

var bootstrap = SquidStdBootstrap.Create()
    .ConfigureServices(container => container.AddInMemoryCache());

await bootstrap.StartAsync();

var eventBus = bootstrap.Resolve<IEventBus>();   // core service, always available
var cache    = bootstrap.Resolve<ICacheService>();

// React to domain events…
using var subscription = eventBus.Subscribe<OrderPlaced>(async (e, ct) =>
    await cache.SetAsync($"order:{e.OrderId}", DateTimeOffset.UtcNow, TimeSpan.FromHours(1)));

// …and publish them.
await eventBus.PublishAsync(new OrderPlaced("A-1001"));

await bootstrap.StopAsync();
```

The event bus, config manager, job system, scheduler and metrics are wired by the bootstrapper out
of the box; modules such as caching are opted in through `ConfigureServices`.

## Project templates

Scaffold a ready-to-run project with the `dotnet new` template pack:

```bash
dotnet new install SquidStd.Templates
```

| Template            | Short name            | What you get                                                    |
|---------------------|-----------------------|-----------------------------------------------------------------|
| Console host        | `squidstd-host`       | A `SquidStdBootstrap` console host.                             |
| ASP.NET minimal API | `squidstd-aspnetcore` | `UseSquidStd` + health checks + a sample endpoint + Dockerfile. |
| Worker microservice | `squidstd-worker`     | `AddWorkers` + a sample `IJobHandler` + Dockerfile.             |
| Worker manager      | `squidstd-manager`    | `AddWorkerManager` + `MapWorkerManagerEndpoints` + Dockerfile.  |

```bash
dotnet new squidstd-worker -n Acme.Resizer
dotnet new squidstd-manager -n Acme.Manager --messaging inmemory
```

`--messaging` (`rabbitmq` default | `inmemory`) is available on the worker and manager templates.

## Packages

### Core & hosting

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Core` | Foundational contracts & utilities (config, event bus, jobs, metrics, serialization, YAML/JSON, Serilog sink). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Core/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Core.svg)](https://www.nuget.org/packages/SquidStd.Core/) |
| `SquidStd.Abstractions` | DI registration plumbing (`ISquidStdService`, `RegisterStdService`, `RegisterConfigSection`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Abstractions/) |
| `SquidStd.Generators` | Roslyn source generators for event listener, service, config, worker, and Lua registration helpers. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Generators/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Generators.svg)](https://www.nuget.org/packages/SquidStd.Generators/) |
| `SquidStd.Services.Core` | Concrete services: config, event bus, jobs, timer/cron scheduler, dispatcher, metrics, health checks, secrets. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Services.Core/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Services.Core.svg)](https://www.nuget.org/packages/SquidStd.Services.Core/) |
| `SquidStd.AspNetCore` | ASP.NET Core host integration for the SquidStd service stack. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.AspNetCore/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.AspNetCore.svg)](https://www.nuget.org/packages/SquidStd.AspNetCore/) |
| `SquidStd.Plugin.Abstractions` | Plugin contracts (`ISquidStdPlugin`, metadata, context). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Plugin.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Plugin.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Plugin.Abstractions/) |

### Networking

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Network` | TCP/UDP servers & clients, sessions, framing/middleware pipeline, span readers/writers. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Network/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Network.svg)](https://www.nuget.org/packages/SquidStd.Network/) |

### Messaging & actors

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Messaging.Abstractions` | Messaging contracts (`IMessageQueue`, `IQueueProvider`, serializer/metrics, listeners). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Messaging.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Messaging.Abstractions/) |
| `SquidStd.Messaging` | In-memory messaging transport (`AddInMemoryMessaging`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Messaging/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.svg)](https://www.nuget.org/packages/SquidStd.Messaging/) |
| `SquidStd.Messaging.RabbitMq` | RabbitMQ messaging transport (`AddRabbitMqMessaging`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Messaging.RabbitMq/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.RabbitMq.svg)](https://www.nuget.org/packages/SquidStd.Messaging.RabbitMq/) |
| `SquidStd.Messaging.Sqs` | AWS SQS/SNS transport: `IQueueProvider` over SQS (redrive→DLQ) and `ITopicProvider` via SNS+SQS fan-out (`AddSqsMessaging`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Messaging.Sqs/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Messaging.Sqs.svg)](https://www.nuget.org/packages/SquidStd.Messaging.Sqs/) |
| `SquidStd.Actors` | Ordered, single-threaded, lock-free per-entity message processing on TPL Dataflow (`Actor<T>`, `TellAsync`/`AskAsync`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Actors/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Actors.svg)](https://www.nuget.org/packages/SquidStd.Actors/) |

### Persistence & database

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Persistence.Abstractions` | Binary-persistence contracts (`IEntityStore<T,TKey>`, `IPersistenceService`, journal/snapshot/registry, `PersistenceConfig`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Persistence.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Persistence.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Persistence.Abstractions/) |
| `SquidStd.Persistence` | In-memory entity store with durable binary snapshot + journal (WAL); serializer-agnostic engine. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Persistence/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Persistence.svg)](https://www.nuget.org/packages/SquidStd.Persistence/) |
| `SquidStd.Persistence.MessagePack` | MessagePack-backed binary `IDataSerializer` for SquidStd.Persistence (recommended default). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Persistence.MessagePack/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Persistence.MessagePack.svg)](https://www.nuget.org/packages/SquidStd.Persistence.MessagePack/) |
| `SquidStd.Database.Abstractions` | Provider-agnostic data-access contracts (`IDataAccess<T>`, `BaseEntity`, `PagedResultData`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Database.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Database.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Database.Abstractions/) |
| `SquidStd.Database` | FreeSql-backed data access (CRUD/bulk/paging, URI connection strings, ZLinq helpers). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Database/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Database.svg)](https://www.nuget.org/packages/SquidStd.Database/) |

### Caching

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Caching.Abstractions` | Caching contracts (`ICacheService`, `ICacheProvider`, `CacheService` facade, metrics, connection string). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Caching.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Caching.Abstractions/) |
| `SquidStd.Caching` | In-memory cache backend (`AddInMemoryCache`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Caching/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.svg)](https://www.nuget.org/packages/SquidStd.Caching/) |
| `SquidStd.Caching.Redis` | Redis cache backend (`AddRedisCache`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Caching.Redis/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Caching.Redis.svg)](https://www.nuget.org/packages/SquidStd.Caching.Redis/) |

### Storage & virtual filesystem

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Storage.Abstractions` | Storage contracts (`IStorageService`, `IObjectStorageService`, `StorageConfig`, `ListKeysAsync`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Storage.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Storage.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Storage.Abstractions/) |
| `SquidStd.Storage` | Local file storage backend (`AddFileStorage`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Storage/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Storage.svg)](https://www.nuget.org/packages/SquidStd.Storage/) |
| `SquidStd.Storage.S3` | S3/MinIO storage backend (`AddS3Storage`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Storage.S3/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Storage.S3.svg)](https://www.nuget.org/packages/SquidStd.Storage.S3/) |
| `SquidStd.Vfs.Abstractions` | Virtual filesystem contracts (`IVirtualFileSystem`, `ILockableFileSystem`, `VfsPath`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Vfs.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Vfs.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Vfs.Abstractions/) |
| `SquidStd.Vfs` | Virtual filesystem providers — physical, in-memory, and zip — plus composable decorators (`ReadOnlyFileSystem`, `ScopedFileSystem`, `OverlayFileSystem`, `CachingFileSystem`) and `VfsDirectories`. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Vfs/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Vfs.svg)](https://www.nuget.org/packages/SquidStd.Vfs/) |
| `SquidStd.Vfs.S3` | S3-compatible VFS backend — AWS S3, MinIO, Cloudflare R2, Backblaze B2 — via the MinIO SDK (`RegisterS3FileSystem`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Vfs.S3/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Vfs.S3.svg)](https://www.nuget.org/packages/SquidStd.Vfs.S3/) |
| `SquidStd.Vfs.Database` | Database-backed VFS storing files as rows via SquidStd.Database / FreeSql (`RegisterDatabaseFileSystem`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Vfs.Database/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Vfs.Database.svg)](https://www.nuget.org/packages/SquidStd.Vfs.Database/) |

### Security — crypto & secrets

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Crypto` | OpenPGP key management/operations over an indexed keyring (`SquidStd.Crypto.Pgp`, `RegisterPgp`), password-based encryption (`PasswordCipher`, Argon2id + AES-256-GCM with a self-describing envelope), and the encrypted VFS vault decorator. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Crypto/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Crypto.svg)](https://www.nuget.org/packages/SquidStd.Crypto/) |
| `SquidStd.Secrets.Aws` | AWS adapters for the secret seams — KMS envelope `ISecretProtector` and Secrets Manager `ISecretStore`. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Secrets.Aws/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Secrets.Aws.svg)](https://www.nuget.org/packages/SquidStd.Secrets.Aws/) |

### Search

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Search.Abstractions` | Search/indexing contracts (`IIndexableEntity`, `[SearchIndex]`, `ISearchService`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Search.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Search.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Search.Abstractions/) |
| `SquidStd.Search.Elasticsearch` | Elasticsearch indexing + constrained LINQ query provider (`AddElasticsearch`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Search.Elasticsearch/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Search.Elasticsearch.svg)](https://www.nuget.org/packages/SquidStd.Search.Elasticsearch/) |

### Mail

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Mail.Abstractions` | Mail contracts (`MailMessage`, `MailReceivedEvent`, `IMailReader`, `MailOptions`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Mail.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Mail.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Mail.Abstractions/) |
| `SquidStd.Mail.MailKit` | IMAP/POP3 mail poller that publishes `MailReceivedEvent` (`AddMail`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Mail.MailKit/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Mail.MailKit.svg)](https://www.nuget.org/packages/SquidStd.Mail.MailKit/) |
| `SquidStd.Mail.Queue` | Outbound mail send queue over the messaging queue (`AddMailQueue`, `IMailQueue`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Mail.Queue/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Mail.Queue.svg)](https://www.nuget.org/packages/SquidStd.Mail.Queue/) |

### Workers

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Workers.Abstractions` | Worker/manager shared contracts (`JobRequest`, `WorkerHeartbeat`, `WorkerInfo`, `WorkerChannels`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Workers.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Workers.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Workers.Abstractions/) |
| `SquidStd.Workers` | Worker runtime: consume jobs, dispatch to `IJobHandler`s, publish heartbeats (`AddWorkers`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Workers/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Workers.svg)](https://www.nuget.org/packages/SquidStd.Workers/) |
| `SquidStd.Workers.Manager` | Job enqueue, heartbeat registry, offline sweep, opt-in ASP.NET endpoints (`AddWorkerManager`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Workers.Manager/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Workers.Manager.svg)](https://www.nuget.org/packages/SquidStd.Workers.Manager/) |

### User interface

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Tui` | MVVM for terminal apps on Terminal.Gui v2 — observable ViewModels, a fluent binder (+ `AutoBind` and a declarative DSL), ViewModel-first navigation, DryIoc wiring. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Tui/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Tui.svg)](https://www.nuget.org/packages/SquidStd.Tui/) |

### Scripting & templating

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Scripting.Lua` | Lua scripting engine with attribute-based modules and event bridging. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Scripting.Lua/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Scripting.Lua.svg)](https://www.nuget.org/packages/SquidStd.Scripting.Lua/) |
| `SquidStd.Templating` | Scriban templating with a named-template registry and `templates/*.tmpl` auto-load (`AddTemplating`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Templating/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Templating.svg)](https://www.nuget.org/packages/SquidStd.Templating/) |

### Telemetry

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Telemetry.Abstractions` | Shared telemetry config (`TelemetryOptions`, OTLP protocol, `SquidStd.*` ActivitySource convention). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Telemetry.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Telemetry.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Telemetry.Abstractions/) |
| `SquidStd.Telemetry.OpenTelemetry` | OpenTelemetry tracing + metrics export (OTLP/console), standard instrumentation, metrics-snapshot bridge (`AddSquidStdTelemetry`). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Telemetry.OpenTelemetry/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Telemetry.OpenTelemetry.svg)](https://www.nuget.org/packages/SquidStd.Telemetry.OpenTelemetry/) |

### Shared & tooling

| Package | Description | Links |
|---------|-------------|-------|
| `SquidStd.Aws.Abstractions` | Shared AWS connection config (`AwsConfigEntry`: region, credentials, endpoint override) for AWS-SDK providers. | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Aws.Abstractions/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Aws.Abstractions.svg)](https://www.nuget.org/packages/SquidStd.Aws.Abstractions/) |
| `SquidStd.Templates` | `dotnet new` templates for scaffolding SquidStd projects (host, ASP.NET, worker, manager). | [![readme](https://img.shields.io/badge/readme-1390A3.svg)](src/SquidStd.Templates/README.md) · [![NuGet](https://img.shields.io/nuget/v/SquidStd.Templates.svg)](https://www.nuget.org/packages/SquidStd.Templates/) |

## Related projects

- **[Felix Network](https://github.com/tgiachi/SquidStd-Felix)** — a standalone secure binary
  mesh-networking library for .NET (and a constrained C/ESP32 target): AES-256-GCM encrypted,
  optionally DEFLATE-compressed messages over ENet (reliable UDP) behind a portable frame, with an
  optional self-forming mesh layer and a pluggable transport (`ITransport`). Published as
  `SquidStd.Felix`, `SquidStd.Felix.MemoryPack`, `SquidStd.Felix.Mesh` and
  `SquidStd.Felix.Transport.Serial` on NuGet. See the [Felix Network guide](docs/articles/felix.md)
  for an overview.

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

Integration tests need Docker running; Testcontainers starts disposable RabbitMQ, Redis,
Elasticsearch, MinIO and LocalStack containers automatically.

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

## Built on

squid-std stands on a small set of well-established libraries:

- **DI & logging** — [DryIoc](https://github.com/dadhi/DryIoc), [Serilog](https://serilog.net/)
- **Data** — [FreeSql](https://github.com/dotnetcore/FreeSql), [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp), [YamlDotNet](https://github.com/aaubry/YamlDotNet)
- **Messaging & cache** — [RabbitMQ.Client](https://github.com/rabbitmq/rabbitmq-dotnet-client), [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis), [AWS SDK for .NET](https://github.com/aws/aws-sdk-net)
- **Search, mail & storage** — [Elastic.Clients.Elasticsearch](https://github.com/elastic/elasticsearch-net), [MailKit](https://github.com/jstedfast/MailKit), [Minio](https://github.com/minio/minio-dotnet)
- **Crypto** — [PgpCore](https://github.com/mattosaurus/PgpCore) over [BouncyCastle](https://www.bouncycastle.org/)
- **Scripting & templating** — [MoonSharp](https://www.moonsharp.org/) (Lua), [Scriban](https://github.com/scriban/scriban)
- **Observability** — [OpenTelemetry](https://opentelemetry.io/)
- **Scheduling** — [Cronos](https://github.com/HangfireIO/Cronos)

## License

MIT - see [LICENSE](LICENSE).
