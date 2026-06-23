# Serialization

SquidStd uses a single serialization abstraction across the framework, defined in `SquidStd.Core`:

- `IDataSerializer` — `ReadOnlyMemory<byte> Serialize<T>(T value)`
- `IDataDeserializer` — `T Deserialize<T>(ReadOnlyMemory<byte> data)`

The default implementation, `JsonDataSerializer`, uses `System.Text.Json` Web defaults and implements
both interfaces. It is registered by `RegisterCoreServices()` (via `RegisterDataSerializer()`), so both
contracts resolve to the same singleton. Messaging and caching reuse it for payload (de)serialization.

```csharp
using DryIoc;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Services.Core.Extensions;

var container = new Container();
container.RegisterDataSerializer();

var serializer = container.Resolve<IDataSerializer>();
var deserializer = container.Resolve<IDataDeserializer>();

var bytes = serializer.Serialize(new { Name = "squid", Port = 9000 });
var value = deserializer.Deserialize<Dictionary<string, object>>(bytes);
```

To plug in a different format, implement `IDataSerializer` / `IDataDeserializer` and register your type
before `RegisterCoreServices()` (it keeps an already-registered serializer).
