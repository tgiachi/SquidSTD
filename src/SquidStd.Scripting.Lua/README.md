<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Scripting.Lua</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Scripting.Lua/"><img src="https://img.shields.io/nuget/v/SquidStd.Scripting.Lua.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Scripting.Lua.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/scripting-lua.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Lua scripting for SquidStd. Hosts a Lua engine (`IScriptEngineService`) that exposes .NET methods to
scripts through attribute-decorated modules, bridges events, generates `.luarc` typings/docs, and
supports init scripts, constants, and callbacks.

## Install

```bash
dotnet add package SquidStd.Scripting.Lua
```

## Features

- `IScriptEngineService` — load and run Lua scripts; register modules, constants, callbacks, init scripts.
- Attribute-based modules: mark a class `[ScriptModule]` and methods `[ScriptFunction]` to expose them.
- `container.RegisterScriptModule<TModule>()` / `RegisterLuaUserData<T>()` registration extensions.
- Event bridging to the SquidStd event bus (`ILuaEventBridge`).
- Built-in modules (logging, events, random) and `.luarc` documentation generation.

## Usage

```csharp
using DryIoc;
using SquidStd.Scripting.Lua.Attributes.Scripts;
using SquidStd.Scripting.Lua.Extensions.Scripts;

[ScriptModule("math2")]
public sealed class MathModule
{
    [ScriptFunction("add")]
    public int Add(int a, int b) => a + b;
}

var container = new Container();
container.RegisterScriptModule<MathModule>();
// Resolve IScriptEngineService to load and execute scripts that call math2.add(1, 2).
```

## Key types

| Type | Purpose |
|------|---------|
| `IScriptEngineService` | Lua engine: load/run scripts, register modules/constants/callbacks. |
| `ILuaEventBridge` | Bridges Lua scripts to the event bus. |
| `ScriptModuleAttribute` / `ScriptFunctionAttribute` | Expose .NET classes/methods to Lua. |
| `AddScriptModuleExtension` | `RegisterScriptModule<T>()` / `RegisterLuaUserData<T>()`. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
