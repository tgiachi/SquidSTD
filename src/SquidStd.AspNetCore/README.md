<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.AspNetCore</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.AspNetCore/"><img src="https://img.shields.io/nuget/v/SquidStd.AspNetCore.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.AspNetCore.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/aspnetcore.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

ASP.NET Core integration for SquidStd. A single `builder.UseSquidStd(...)` call wires the SquidStd
DryIoc container into the web host and registers a hosted service that starts and stops every
`ISquidStdService` alongside the application lifecycle.

## Install

```bash
dotnet add package SquidStd.AspNetCore
```

## Usage

```csharp
using SquidStd.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.UseSquidStd(options =>
{
    // configure SquidStd options here
});

var app = builder.Build();
app.Run();
```

### Health checks

Bridge your SquidStd health checks into the standard `/health` endpoint:

```csharp
using SquidStd.AspNetCore.Extensions;
using SquidStd.Services.Core.Extensions;

builder.UseSquidStd(options => { }, container => container.RegisterHealthChecksService());
builder.AddSquidStdHealthChecks(); // call after UseSquidStd

var app = builder.Build();
app.MapHealthChecks("/health"); // standard ASP.NET Core endpoint
```

Each registered `IHealthCheck` appears as its own entry in the report. Check names must be unique.

## Key types

| Type                                  | Purpose                                                                |
|---------------------------------------|------------------------------------------------------------------------|
| `SquidStdAspNetCoreBuilderExtensions` | `UseSquidStd(...)` builder extension.                                  |
| `SquidStdHostedService`               | Hosted service bridging SquidStd service lifecycle to the host.        |
| `SquidStdHealthChecksExtensions`      | `AddSquidStdHealthChecks(...)` — bridge to ASP.NET Core health checks. |

## Related

- Tutorial: [Build an ASP.NET Core app](https://tgiachi.github.io/squid-std/tutorials/aspnetcore-app.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
