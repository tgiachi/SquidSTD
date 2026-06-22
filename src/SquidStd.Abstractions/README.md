<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/SquidStd/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Abstractions</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Abstractions/"><img src="https://img.shields.io/nuget/v/SquidStd.Abstractions.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Abstractions.svg" alt="Downloads" />
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

DryIoc-based dependency-injection plumbing for SquidStd. It defines the `ISquidStdService` lifecycle
contract and the container extensions used to register services and configuration sections in a uniform,
discoverable way (tracked through ordered registration lists).

## Install

```bash
dotnet add package SquidStd.Abstractions
```

## Features

- `ISquidStdService` — a `StartAsync`/`StopAsync` lifecycle contract for managed services.
- `RegisterStdService<TService, TImplementation>()` — register a singleton service and record it in the
  ordered service list (with optional priority).
- `RegisterConfigSection<TConfig>(sectionName)` — register a YAML config section for the config manager.
- `AddToRegisterTypedList(...)` — maintain ordered registration lists in the container.

## Usage

```csharp
using DryIoc;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Abstractions.Extensions.Services;

var container = new Container();

container.RegisterStdService<IMyService, MyService>();
container.RegisterConfigSection<MyConfig>("my");
```

## Key types

| Type | Purpose |
|------|---------|
| `ISquidStdService` | Async start/stop lifecycle for managed services. |
| `RegisterStdServiceExtension` | `RegisterStdService<,>` container extension. |
| `RegisterConfigSectionExtension` | `RegisterConfigSection<>` container extension. |
| `ServiceRegistrationData` | Ordered service registration record. |
| `ConfigRegistrationData` | Config section registration record. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/SquidStd).
