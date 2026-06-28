# Getting Started

Install the package(s) you need and bootstrap the core services.

```bash
dotnet add package SquidStd.Services.Core
```

```csharp
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Services.Core.Services.Bootstrap;

// Core services wired automatically: config manager, event bus, command dispatcher,
// job system, timer/cron scheduler, metrics, health checks, storage and secrets.
var bootstrap = SquidStdBootstrap.Create(
    new SquidStdOptions { ConfigName = "squidstd", RootDirectory = AppContext.BaseDirectory });

await bootstrap.StartAsync();
// … resolve services, opt into modules with bootstrap.ConfigureServices(…) …
await bootstrap.StopAsync();
```

Opt into modules with `ConfigureServices`, e.g. `container.AddInMemoryCache()`. See the
[Concepts](concepts/bootstrap-lifecycle.md) for the full lifecycle and
[Packages](getting-started.md) for per-module reference.
