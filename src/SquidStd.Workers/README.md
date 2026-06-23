<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Workers</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Workers/"><img src="https://img.shields.io/nuget/v/SquidStd.Workers.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Workers.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/workers.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

The SquidStd worker runtime. Consumes `JobRequest`s from the jobs queue, dispatches each to a named
`IJobHandler` (up to `MaxConcurrency` in parallel), and publishes a `WorkerHeartbeat` on the heartbeat topic
every few seconds.

## Install

```bash
dotnet add package SquidStd.Workers
```

## Usage

```csharp
using DryIoc;
using SquidStd.Workers.Extensions;

container.AddInMemoryMessaging(); // or AddRabbitMqMessaging(...)
container.AddWorkers();
container.AddJobHandler<ResizeImageHandler>();
```

```csharp
public sealed class ResizeImageHandler : IJobHandler
{
    public string JobName => "resize-image";

    public Task HandleAsync(JobRequest job, CancellationToken cancellationToken)
    {
        // job.Parameters["width"], ...
        return Task.CompletedTask;
    }
}
```

## Key types

| Type | Purpose |
|------|---------|
| `IJobHandler` | Handles jobs of one named kind. |
| `WorkersConfig` | `WorkerId`, `HeartbeatIntervalSeconds`, `MaxConcurrency`, queue/topic names. |
| `WorkersRegistrationExtensions` | `AddWorkers()` and `AddJobHandler<T>()`. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
