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

## Usage

```csharp
using DryIoc;
using SquidStd.Services.Core.Extensions;

var container = new Container();

// Registers config manager + event bus + jobs + timer wheel + dispatcher + metrics + storage + secrets.
container.RegisterCoreServices("squidstd", Directory.GetCurrentDirectory());
```

### Command dispatch

```csharp
// Register a dispatcher for your context type and handlers.
container.RegisterCommandDispatcher<Session>();
container.RegisterCommandHandler<PingCommand, Session, PingHandler>();

// Dispatch (handlers auto-subscribed at bootstrap). Pass the context explicitly:
var result = await dispatcher.DispatchAsync(new PingCommand("hi"), session);
if (!result.Matched)
{
    // unknown command
}
```

For network-driven dispatch, build the context from a seed (e.g. the originating connection):

```csharp
container.RegisterCommandDispatcher<Session>();
container.RegisterCommandHandler<PingCommand, Session, PingHandler>();
container.RegisterSeededCommandDispatcher<Session, Connection, ConnectionSessionFactory>();

// in the receive loop, where the connection is known:
var seeded = container.Resolve<ISeededCommandDispatcher<Session, Connection>>();
await seeded.DispatchAsync(command, connection);   // factory maps connection -> Session
```

## Key types

| Type                                | Purpose                                                                   |
|-------------------------------------|---------------------------------------------------------------------------|
| `RegisterDefaultServicesExtensions` | `RegisterCoreServices()` / `RegisterConfigManagerService()` entry points. |
| `ConfigManagerService`              | YAML config load/save with env-var substitution.                          |
| `EventBusService`                   | In-process event bus implementation.                                      |
| `CommandDispatcher<TContext>`       | Typed protocol command dispatch with context.                             |
| `JobSystemService`                  | Background job execution.                                                 |
| `TimerWheelService`                 | Timer-wheel scheduling.                                                   |
| `MainThreadDispatcherService`       | Main-thread work dispatch.                                                |
| `MetricsCollectionService`          | Metric sample aggregation.                                                |

## Related

- Tutorial: [Events, jobs & scheduling](https://tgiachi.github.io/squid-std/tutorials/events-jobs-scheduling.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
