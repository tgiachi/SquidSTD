<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Services.Core</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Services.Core/"><img src="https://img.shields.io/nuget/v/SquidStd.Services.Core.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Services.Core.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/services-core.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Concrete implementations of the SquidStd.Core contracts, wired for DryIoc. A single
`RegisterCoreServices()` call brings up the configuration manager, event bus, job system, timer wheel,
main-thread dispatcher, metrics collection, storage, and secrets services.

## Install

```bash
dotnet add package SquidStd.Services.Core
```

## Features

- One-line bootstrap: `container.RegisterCoreServices()` registers the full default service set.
- `ConfigManagerService` — loads/saves YAML config sections and substitutes `$ENV_VAR` tokens.
- `EventBusService` — in-process publish/subscribe over `IEvent`.
- `JobSystemService` — background job execution; `TimerWheelService` + cron scheduling for timed work.
- `MainThreadDispatcherService` — marshal work back onto a main thread.
- `MetricsCollectionService` — aggregates `IMetricProvider` samples.
- File storage, object storage, and AES-GCM-protected secret store.

## Usage

```csharp
using DryIoc;
using SquidStd.Services.Core.Extensions;

var container = new Container();

// Registers config manager + event bus + jobs + timer wheel + dispatcher + metrics + storage + secrets.
container.RegisterCoreServices("squidstd", Directory.GetCurrentDirectory());
```

## Key types

| Type | Purpose |
|------|---------|
| `RegisterDefaultServicesExtensions` | `RegisterCoreServices()` / `RegisterConfigManagerService()` entry points. |
| `ConfigManagerService` | YAML config load/save with env-var substitution. |
| `EventBusService` | In-process event bus implementation. |
| `JobSystemService` | Background job execution. |
| `TimerWheelService` | Timer-wheel scheduling. |
| `MainThreadDispatcherService` | Main-thread work dispatch. |
| `MetricsCollectionService` | Metric sample aggregation. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
