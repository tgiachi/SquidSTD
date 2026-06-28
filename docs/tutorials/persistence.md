# Persistence

Keep entities in an in-memory store backed by a durable binary snapshot plus a journal, so state
survives a restart.

## What you'll build

A standalone demo of `SquidStd.Persistence`: a `Player` store that loads existing state on
startup, appends every change to a journal, and captures a snapshot. Run it twice — the second run
reloads what the first saved.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Persistence` (and `SquidStd.Persistence.MessagePack` for the
  binary serializer)

## Steps

### 1. Initialize and load existing state

`InitializeAsync` replays the snapshot and journal from the save directory; `GetStore<T, TKey>`
returns the typed store for an entity registered in the `PersistenceEntityRegistry`.

[!code-csharp[](../../samples/SquidStd.Samples.Persistence/Program.cs#step-1)]

### 2. Mutate the store

Every `UpsertAsync` / `RemoveAsync` is appended to the journal, so the change is durable before
the next snapshot.

[!code-csharp[](../../samples/SquidStd.Samples.Persistence/Program.cs#step-2)]

### 3. Snapshot and trim the journal

`SaveSnapshotAsync` captures the full state and trims the journal. Re-run the sample to see the
state reload.

[!code-csharp[](../../samples/SquidStd.Samples.Persistence/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Persistence
dotnet run --project samples/SquidStd.Samples.Persistence   # reloads the saved state
```

## Next steps

- [SquidStd.Persistence reference](../articles/persistence.md)
