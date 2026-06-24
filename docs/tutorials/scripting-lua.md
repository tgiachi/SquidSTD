# Scripting with Lua

Embed a Lua engine in your host, expose C# values to scripts, and read results back.

## What you'll build

A console host that registers the `IScriptEngineService` (`SquidStd.Scripting.Lua`) through the SquidStd
bootstrap, runs a small Lua script, and prints values evaluated by the engine.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Scripting.Lua`
- `dotnet add package SquidStd.Services.Core`

## Steps

### 1. Register the Lua engine

The engine needs a `LuaEngineConfig` (it watches a scripts directory) and is started by the bootstrap as a
SquidStd service. The `DirectoriesConfig` it depends on is already registered by the core services.

[!code-csharp[](../../samples/SquidStd.Samples.ScriptingLua/Program.cs#step-1)]

### 2. Run a script and read results

`RegisterGlobal` exposes a C# value under an exact name, `ExecuteScript` runs Lua code, and
`ExecuteFunction` evaluates an expression and returns a `ScriptResult` whose `Data` holds the value.

[!code-csharp[](../../samples/SquidStd.Samples.ScriptingLua/Program.cs#step-2)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.ScriptingLua
```

Prints:

```
3 + 4 = 7
result = hello from C# and lua
```

## How it works

`IScriptEngineService` wraps a MoonSharp `Script`. Registering it with `RegisterStdService` lets the bootstrap
call its `StartAsync` during `StartAsync`, which wires up modules, constants, and a file watcher over the
configured scripts directory. Globals you register become Lua variables, and `ExecuteFunction` evaluates a
`return <expression>` and surfaces the value through `ScriptResult.Data`.

## See also

- [SquidStd.Scripting.Lua reference](../articles/scripting-lua.md)
