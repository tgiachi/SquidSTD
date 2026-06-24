# Getting started: your first SquidStd host

Build a minimal SquidStd application: a bootstrapper that loads config, configures logging, and runs the
registered services until shutdown.

## What you'll build

A console host on top of `SquidStdBootstrap` (from `SquidStd.Services.Core`, which brings in `SquidStd.Core`
and `SquidStd.Abstractions`). It loads its config, wires the default services, and runs until you stop it.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Services.Core`

## Steps

### 1. Create the bootstrapper

`SquidStdBootstrap.Create` takes a `SquidStdOptions` (config name + root directory) and registers the core
services (config, logging, event bus, jobs, timers…) into an owned DryIoc container.

[!code-csharp[](../../samples/SquidStd.Samples.GettingStarted/Program.cs#step-1)]

### 2. Start the services

`StartAsync` starts every registered `ISquidStdService` in priority order.

[!code-csharp[](../../samples/SquidStd.Samples.GettingStarted/Program.cs#step-2)]

### 3. Run until shutdown

`RunAsync` starts (if not already started), waits for cancellation, then stops the services cleanly. Use either
`StartAsync` plus your own loop, or just `RunAsync`.

[!code-csharp[](../../samples/SquidStd.Samples.GettingStarted/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.GettingStarted
```

The host starts, logs the service lifecycle, and waits until you press Ctrl+C.

## How it works

`SquidStdBootstrap` is the composition root: it builds the container, registers the core services, loads the
config sections, and orchestrates the `ISquidStdService` lifecycle. Every other SquidStd module plugs into this
container through its `Add…` extension methods.

## See also

- [SquidStd.Services.Core reference](../articles/services-core.md)
- [SquidStd.Core reference](../articles/core.md)
- Next: [Events, jobs and scheduling](events-jobs-scheduling.md)
