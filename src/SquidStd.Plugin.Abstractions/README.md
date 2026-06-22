<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Plugin.Abstractions</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Plugin.Abstractions/"><img src="https://img.shields.io/nuget/v/SquidStd.Plugin.Abstractions.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Plugin.Abstractions.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/plugin-abstractions.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Contracts for building SquidStd plugins. A plugin declares its identity through `PluginMetadata` and
registers its services into the host's DryIoc container via `Configure`, receiving a `PluginContext`
with shared boot data.

## Install

```bash
dotnet add package SquidStd.Plugin.Abstractions
```

## Features

- `ISquidStdPlugin` — the plugin entry point: `Metadata` + `Configure(IContainer, PluginContext)`.
- `PluginMetadata` — id, name, `Version`, author, optional description, and dependency declarations.
- `PluginContext` — a typed bag of boot data shared with the plugin (`GetData<T>(key)`).

## Usage

```csharp
using DryIoc;
using SquidStd.Plugin.Abstractions.Data;
using SquidStd.Plugin.Abstractions.Interfaces.Plugins;

public sealed class MyPlugin : ISquidStdPlugin
{
    public PluginMetadata Metadata { get; } = new()
    {
        Id = "com.example.myplugin",
        Name = "My Plugin",
        Version = new Version(1, 0, 0),
        Author = "me"
    };

    public void Configure(IContainer container, PluginContext context)
    {
        // register the plugin's services / config sections here
    }
}
```

## Key types

| Type | Purpose |
|------|---------|
| `ISquidStdPlugin` | Plugin entry point (`Metadata`, `Configure`). |
| `PluginMetadata` | Plugin identity and dependency declarations. |
| `PluginContext` | Shared boot data passed to the plugin. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
