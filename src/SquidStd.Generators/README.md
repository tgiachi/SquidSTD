# SquidStd.Generators

Roslyn source generators for SquidStd.

The first generator discovers concrete `IEventListener<TEvent>` implementations in the consuming project and generates a DryIoc registration extension:

```csharp
container.RegisterGeneratedEventListeners();
```

The generated method reuses the normal `RegisterEventListener<TEvent,TListener>()` runtime path, so listener activation stays compatible with `SquidStd.Services.Core`.
