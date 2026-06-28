<h1 align="center">SquidStd.Generators</h1>

Roslyn source generators for SquidStd compile-time registration helpers.

## Install

```bash
dotnet add package SquidStd.Generators
```

## Usage

The event listener generator discovers concrete `IEventListener<TEvent>` implementations marked with `[RegisterEventListener]` and generates a DryIoc registration extension:

```csharp
using SquidStd.Abstractions.Attributes;
using SquidStd.Core.Interfaces.Events;

[RegisterEventListener]
public sealed class PingListener : IEventListener<PingEvent>
{
    public Task HandleAsync(PingEvent eventData, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

container.RegisterGeneratedEventListeners();
```

The generated method reuses the normal `RegisterEventListener<TEvent,TListener>()` runtime path, so listener activation stays compatible with `SquidStd.Services.Core`. Each registration family has its own marker attribute and generated extension method, all calling the same runtime APIs as manual registration (`RegisterStdService`, `RegisterConfigSection`, `AddJobHandler`, `RegisterScriptModule`).

## Key types

| Marker attribute | Generated method |
|------------------|------------------|
| `[RegisterEventListener]` | `RegisterGeneratedEventListeners()` |
| `[RegisterStdService(typeof(IMyService), Priority = 10)]` | `RegisterGeneratedStdServices()` |
| `[RegisterConfigSection("workers", Priority = -50)]` | `RegisterGeneratedConfigSections()` |
| `[RegisterJobHandler]` | `RegisterGeneratedJobHandlers()` |
| `[RegisterScriptModule]` with `[ScriptModule("name")]` | `RegisterGeneratedScriptModules()` |

## Related

- Tutorial: [Source generators: event listeners](https://tgiachi.github.io/squid-std/tutorials/source-generators-event-listeners.html)
- Tutorial: [Source generators: registration](https://tgiachi.github.io/squid-std/tutorials/source-generators-registration.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
