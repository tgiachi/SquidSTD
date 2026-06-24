# Host SquidStd in an ASP.NET Core app

Wire SquidStd into an ASP.NET Core Minimal API and expose its health checks over HTTP.

## What you'll build

A Minimal API web app that uses SquidStd (`SquidStd.AspNetCore`) as its DryIoc-backed service provider, surfaces
the registered SquidStd health checks at `/health`, and answers a simple root endpoint.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.AspNetCore`

## Steps

### 1. Register SquidStd on the builder

`UseSquidStd` swaps in DryIoc as the ASP.NET Core service provider and bootstraps SquidStd. The root directory is
taken from the host environment automatically, so you only set the config name.

[!code-csharp[](../../samples/SquidStd.Samples.AspNetCore/Program.cs#step-1)]

### 2. Bridge the health checks

`AddSquidStdHealthChecks` registers every SquidStd `IHealthCheck` as a standard ASP.NET Core health check. Call it
after `UseSquidStd`.

[!code-csharp[](../../samples/SquidStd.Samples.AspNetCore/Program.cs#step-2)]

### 3. Build the app and map endpoints

Build the app, expose the health checks with the standard `MapHealthChecks`, add a root endpoint, and run.

[!code-csharp[](../../samples/SquidStd.Samples.AspNetCore/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.AspNetCore
```

Browse to `/` for the "SquidStd up" message and to `/health` for the aggregated health-check status.

## How it works

`UseSquidStd` creates an owned DryIoc container, registers it through `DryIocServiceProviderFactory`, and hooks the
SquidStd lifecycle into the ASP.NET Core host via a hosted service. `AddSquidStdHealthChecks` resolves the SquidStd
`IHealthCheck` instances from that container and adapts each one into the standard health-check pipeline, so
`MapHealthChecks` reports them like any other ASP.NET Core check.

## See also

- [SquidStd.AspNetCore reference](../articles/aspnetcore.md)
- Previous: [Getting started](getting-started.md)
