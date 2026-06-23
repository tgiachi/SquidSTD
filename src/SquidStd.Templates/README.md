<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Templates</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Templates/"><img src="https://img.shields.io/nuget/v/SquidStd.Templates.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Templates.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/templates.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

`dotnet new` templates for scaffolding SquidStd projects. The generated projects reference the SquidStd packages
at the same version as this template pack.

## Install

```bash
dotnet new install SquidStd.Templates
```

## Templates

| Template | Short name | What you get |
|----------|------------|--------------|
| Console host | `squidstd-host` | A `SquidStdBootstrap` console host. |
| ASP.NET minimal API | `squidstd-aspnetcore` | `UseSquidStd` + health checks + a sample endpoint + Dockerfile. |
| Worker microservice | `squidstd-worker` | `AddWorkers` + a sample `IJobHandler` + Dockerfile. |
| Worker manager | `squidstd-manager` | `AddWorkerManager` + `MapWorkerManagerEndpoints` + Dockerfile. |

## Usage

```bash
dotnet new squidstd-worker -n Acme.Resizer
dotnet new squidstd-worker -n Acme.Resizer --messaging inmemory
dotnet new squidstd-manager -n Acme.Manager
```

`--messaging` (`rabbitmq` default | `inmemory`) is available on the worker and manager templates.

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
