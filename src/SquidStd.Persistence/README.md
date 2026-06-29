<h1 align="center">SquidStd.Persistence</h1>

Embeddable in-memory entity store with durable **binary snapshot + journal (write-ahead log)** persistence.
Full state lives in memory (synchronous reads), every mutation is appended to a length+checksum-framed
binary journal, and a periodic snapshot captures all state and trims the journal. On startup the engine
loads the snapshot and replays the journal tail. The engine is **serializer-agnostic** (via SquidStd's
`IDataSerializer`/`IDataDeserializer`) and has no MessagePack or domain dependency.

## Install

```bash
dotnet add package SquidStd.Persistence
dotnet add package SquidStd.Persistence.MessagePack   # recommended binary serializer
```

## Usage

```csharp
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.MessagePack;
using SquidStd.Persistence.Services;

public sealed class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

var serializer = new MessagePackDataSerializer();

var registry = new PersistenceEntityRegistry();
registry.Register(new PersistenceEntityDescriptor<Player, int>(
    serializer, serializer, typeId: 1, typeName: "Player", schemaVersion: 1, keySelector: p => p.Id));

var config = new PersistenceConfig { SaveDirectory = "./save" };
var journal = new BinaryJournalService(Path.Combine(config.SaveDirectory, config.JournalFileName));
var snapshot = new SnapshotService(config.SaveDirectory, config.SnapshotFileSuffix);

var persistence = new PersistenceService(registry, journal, snapshot, config);
await persistence.InitializeAsync();                       // load snapshot + replay journal

var players = persistence.GetStore<Player, int>();
await players.UpsertAsync(new Player { Id = 1, Name = "Bob" });   // appended to the journal
await persistence.SaveSnapshotAsync();                            // snapshot + trim journal

var bob = await players.GetByIdAsync(1);                          // detached clone
```

### DI registration

```csharp
container.RegisterPersistedEntity<Player, int>(typeId: 1, typeName: "Player", schemaVersion: 1, p => p.Id);
container.ApplyPersistedEntityRegistrations();   // builds descriptors into IPersistenceEntityRegistry
```

## Key types

| Type                                  | Purpose                                                        |
|---------------------------------------|---------------------------------------------------------------|
| `PersistenceService`                  | Lifecycle: load + replay, autosave, `GetStore<T,TKey>()`.     |
| `IEntityStore<TEntity,TKey>`          | In-memory CRUD; reads clone, writes journal.                  |
| `PersistenceEntityDescriptor<T,TKey>` | Serializer-injected descriptor (serialize/clone/key).         |
| `PersistenceEntityRegistry`           | Maps `typeId` ↔ descriptor; freezes after registration.       |
| `BinaryJournalService`                | Append-only framed binary WAL with tail-corruption recovery.  |
| `SnapshotService`                     | Atomic per-type binary snapshot files with payload checksum.  |
| `RegisterPersistedEntity<T,TKey>()`   | DI helper recording an entity for descriptor construction.    |

### Durability

`PersistenceConfig.DurabilityMode` selects how writes reach disk. `Buffered` (default) flushes to the OS
cache — fast, and safe across a process crash. `Durable` fsyncs each journal append and the snapshot temp
file before its atomic rename, so committed data survives power loss. Pass it through when constructing the
services: `new BinaryJournalService(path, config.DurabilityMode)` and
`new SnapshotService(dir, suffix, config.DurabilityMode)`. (.NET has no portable directory fsync, so the
guarantee is per-file content durability plus atomic rename.)

## Related

- Tutorial: [Persistence](https://tgiachi.github.io/squid-std/tutorials/persistence.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
