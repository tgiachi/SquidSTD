<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/SquidStd/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Core</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Core/"><img src="https://img.shields.io/nuget/v/SquidStd.Core.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Core.svg" alt="Downloads" />
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
- Background work & timing: `IJobSystem`, `ITimerService`, `IMainThreadDispatcher`.
- Metrics & storage: `IMetricProvider`, `IStorageService`, `IObjectStorageService`, secrets contracts.
- Utilities: `YamlUtils`, `JsonUtils`, a Serilog `EventSink`, and string/env/directory extensions.
- Shared domain enums under `Types` (e.g. `LogLevelType`, `PlatformType`).

## Usage

```csharp
using SquidStd.Core.Extensions.Env;
using SquidStd.Core.Yaml;

// Expand "$VAR" tokens against the environment (unknown vars are left untouched).
var path = "$HOME/squidstd/data".ReplaceEnv();

// Serialize / deserialize YAML.
var yaml = YamlUtils.Serialize(new { name = "squid", port = 9000 });
```

## Key types

| Type | Purpose |
|------|---------|
| `IConfigEntry` | A registrable YAML configuration section. |
| `IConfigManagerService` | Loads YAML config and exposes typed sections. |
| `IEventBus` | Publish/subscribe in-process event bus. |
| `IJobSystem` | Background job scheduling/execution. |
| `ITimerService` | Timer-wheel based scheduling. |
| `IMetricProvider` | Source of metric samples for collection. |
| `IStorageService` | File/object storage abstraction. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/SquidStd).
