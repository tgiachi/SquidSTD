# Scaffolding projects with `dotnet new`

Generate ready-to-run SquidStd projects from the command line with the `SquidStd.Templates` pack.

## What you'll build

Nothing by hand — you'll install the template pack and scaffold a console host, an ASP.NET app, a worker
microservice, and a worker manager, each pre-wired to SquidStd.

## Prerequisites

- .NET 10 SDK

## Steps

### 1. Install the template pack

```bash
dotnet new install SquidStd.Templates
```

This registers the SquidStd templates; list them with `dotnet new list squidstd`.

### 2. Scaffold a project

```bash
# console host
dotnet new squidstd-host -n Acme.Host

# ASP.NET minimal API (UseSquidStd + health checks)
dotnet new squidstd-aspnetcore -n Acme.Api

# worker microservice (consumes jobs, publishes heartbeats)
dotnet new squidstd-worker -n Acme.Worker

# worker manager (enqueue jobs + ASP.NET endpoints)
dotnet new squidstd-manager -n Acme.Manager
```

### 3. Pick a messaging transport (worker / manager)

The `squidstd-worker` and `squidstd-manager` templates accept `--messaging`:

```bash
dotnet new squidstd-worker -n Acme.Worker --messaging rabbitmq   # default
dotnet new squidstd-worker -n Acme.Worker --messaging inmemory
```

## Run it

```bash
cd Acme.Worker
dotnet run
```

The generated project references the SquidStd packages at the same version as the template pack and is ready to
build and run.

## How it works

`SquidStd.Templates` is a `dotnet new` template pack. Each template ships a runnable project (with a `Dockerfile`
for the service templates) whose `PackageReference` versions are injected to match the pack, so a scaffolded app
always lines up with the libraries it targets.

## See also

- [SquidStd.Templates reference](../articles/templates.md)
- Related: [A worker system end-to-end](worker-system.md)
