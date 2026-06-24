# Plugins

Implement the SquidStd plugin contract and have a plugin register its own services into the container.

## What you'll build

A console app that defines a plugin implementing `ISquidStdPlugin` (`SquidStd.Plugin.Abstractions`), describes
it with `PluginMetadata`, and calls `Configure` to register a service that the host then resolves.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Plugin.Abstractions`

## Steps

### 1. Implement the plugin contract

A plugin exposes a `PluginMetadata` (its identity and dependencies) and a `Configure` method that registers its
services, handlers, and integrations into the DryIoc container.

[!code-csharp[](../../samples/SquidStd.Samples.Plugins/Program.cs#step-1)]

### 2. Describe, configure, and resolve

The host builds a `PluginContext` for boot-time data, invokes `Configure` on the plugin, and then resolves the
services the plugin registered.

[!code-csharp[](../../samples/SquidStd.Samples.Plugins/Program.cs#step-2)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Plugins
```

Prints:

```
Loading Weather Plugin v1.0.0 by SquidStd Samples
Hello squid, the weather plugin is online.
```

## How it works

`ISquidStdPlugin` is the contract a host loads: `Metadata` is the source of truth for plugin identity and load
order (`Dependencies`), while `Configure(IContainer, PluginContext)` is called during container configuration so
the plugin can register everything it contributes. `PluginContext.Data` carries host-supplied boot values into
the plugin. A real host discovers plugin assemblies and calls `Configure` in dependency order; this sample wires
one plugin by hand so the contract is fully runnable without external assemblies.

## See also

- [SquidStd.Plugin.Abstractions reference](../articles/plugin-abstractions.md)
