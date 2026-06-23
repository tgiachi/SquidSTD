<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Workers.Abstractions</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Workers.Abstractions/"><img src="https://img.shields.io/nuget/v/SquidStd.Workers.Abstractions.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Workers.Abstractions.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/workers-abstractions.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Shared, dependency-free contracts between the SquidStd worker runtime and manager: the job descriptor, the
worker heartbeat, the manager-side worker view, the worker status enum, and the conventional channel names.

## Install

```bash
dotnet add package SquidStd.Workers.Abstractions
```

## Key types

| Type | Purpose |
|------|---------|
| `JobRequest` | A job (name + string parameters) enqueued by the manager and consumed by a worker. |
| `WorkerHeartbeat` | Liveness signal a worker publishes (status, active jobs, max concurrency). |
| `WorkerInfo` | The manager-side view of a worker, folded from heartbeats. |
| `WorkerStatusType` | `Idle` / `Busy` / `Offline`. |
| `WorkerChannels` | Default jobs-queue and heartbeat-topic names. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
