<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Core</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Core/"><img src="https://img.shields.io/nuget/v/SquidStd.Core.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Core.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/core.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Foundational contracts and utilities for the SquidStd stack. It defines the core service interfaces
(configuration, event bus, jobs, timing, metrics, storage) and ships dependency-free helpers — YAML/JSON
serialization, a Serilog event sink, and string/environment/directory extensions — that the other
SquidStd packages build on.

## Install

```bash
dotnet add package SquidStd.Core
```

## Features

- Configuration contracts: `IConfigEntry` (a YAML section) and `IConfigManagerService`.
- In-process messaging: `IEventBus` with `ISyncEventListener<T>` / `IAsyncEventListener<T>` over `IEvent`.
- Command dispatch: `ICommandDispatcher<TContext>` with `ICommandHandler<TCommand,TContext>`, fan-out, fault isolation, and a `CommandDispatchResult`; `ICommandContextFactory<TContext,TSeed>` builds the context from a seed for `ISeededCommandDispatcher<TContext,TSeed>`.
- Background work & timing: `IJobSystem`, `ITimerService`, `IMainThreadDispatcher`.
- Metrics & secrets: `IMetricProvider` and secret-protection contracts.
- Serialization: `IDataSerializer` / `IDataDeserializer` (default `JsonDataSerializer`), plus `YamlUtils` / `JsonUtils`.
- File watching: `IFileWatcherService` / `FileWatcherService` — recursive, debounced watchers that publish `FileChangedEvent` on the event bus.
- Object pooling: `ObjectPool<T>` — thread-safe, non-blocking, factory-based reuse with optional reset.
- Cryptography: `CryptoUtils` (AES-GCM authenticated encrypt/decrypt + key generation), `EncryptString`/`DecryptString` string helpers, base64 extensions, and `SslUtils` for loading PEM/PFX TLS certificates.
- Randomness: `BuiltInRng` (seedable ambient RNG), `RandomUtils` (dice, coin flips), and collection `Shuffle`/`RandomElement`/`RandomSample` extensions.
- Utilities: a Serilog `EventSink`, and string/env/directory extensions.
- Shared domain enums under `Types` (e.g. `LogLevelType`, `PlatformType`, `FileChangeKind`).

## Usage

```csharp
using SquidStd.Core.Extensions.Env;
using SquidStd.Core.Yaml;

// Expand "$VAR" tokens against the environment (unknown vars are left untouched).
var path = "$HOME/squidstd/data".ReplaceEnv();

// Serialize / deserialize YAML.
var yaml = YamlUtils.Serialize(new { name = "squid", port = 9000 });
```

```csharp
using SquidStd.Core.Files;
using SquidStd.Core.Data.Files;
using SquidStd.Core.Pool;

// Watch several directories (each with its own glob) and react via the event bus.
// Via DI: container.RegisterFileWatcherService();  then resolve IFileWatcherService.
var watcher = new FileWatcherService(eventBus);
watcher.Watch("data/scripts", "*.lua");
watcher.Watch("data/templates", "*.json");
eventBus.Subscribe<FileChangedEvent>((change, _) =>
{
    Console.WriteLine($"{change.Kind}: {change.FullPath}");
    return Task.CompletedTask;
});

// Reuse short-lived buffers instead of allocating per call.
using var pool = new ObjectPool<StringBuilder>(() => new StringBuilder(), onReturn: sb => sb.Clear());
var builder = pool.Get();
// ... use builder ...
pool.Return(builder);
```

## Key types

| Type                    | Purpose                                       |
|-------------------------|-----------------------------------------------|
| `IConfigEntry`          | A registrable YAML configuration section.     |
| `IConfigManagerService` | Loads YAML config and exposes typed sections. |
| `IEventBus`             | Publish/subscribe in-process event bus.       |
| `IJobSystem`            | Background job scheduling/execution.          |
| `ITimerService`         | Timer-wheel based scheduling.                 |
| `IMetricProvider`       | Source of metric samples for collection.      |
| `IStorageService`       | File/object storage abstraction.              |
| `IFileWatcherService`   | Recursive, debounced file watcher publishing to the event bus. |
| `ObjectPool<T>`         | Thread-safe, non-blocking object pool.        |
| `ICommandDispatcher<TContext>` | Typed protocol command dispatch with context. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
