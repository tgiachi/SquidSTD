<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Workers.Manager</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Workers.Manager/"><img src="https://img.shields.io/nuget/v/SquidStd.Workers.Manager.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Workers.Manager.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/workers-manager.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

The SquidStd worker manager. Enqueues jobs, collects worker heartbeats into an in-memory registry, marks
workers `Offline` on a periodic sweep, publishes status-transition events, and exposes an opt-in ASP.NET
minimal-API surface.

## Install

```bash
dotnet add package SquidStd.Workers.Manager
```

## Usage

```csharp
using DryIoc;
using SquidStd.Workers.Manager.Extensions;

container.AddInMemoryMessaging(); // or AddRabbitMqMessaging(...)
container.AddWorkerManager();

// In an ASP.NET app, map the opt-in HTTP surface:
app.MapWorkerManagerEndpoints(); // GET /workers, GET /workers/{id}, POST /jobs
```

## Key types

| Type | Purpose |
|------|---------|
| `IJobScheduler` | `EnqueueAsync(jobName, parameters)` onto the jobs queue. |
| `IWorkerRegistry` | `GetAll()` / `Get(id)` over the live worker view. |
| `WorkerStatusChangedEvent` | Published on the event bus on discover / offline / return. |
| `WorkerManagerConfig` | `OfflineTimeoutSeconds`, `SweepIntervalSeconds`, queue/topic names. |
| `WorkerManagerRegistrationExtensions` | `AddWorkerManager()`. |
| `WorkerManagerEndpointsExtensions` | `MapWorkerManagerEndpoints()`. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
