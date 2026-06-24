# Database (FreeSql + SQLite)

Persist a typed entity and read it back with paging, using SQLite with no external database.

## What you'll build

A host that registers the database subsystem (`SquidStd.Database`) and resolves the generic data access
`IDataAccess<TEntity>` (`SquidStd.Database.Abstractions`) to insert and page over a simple entity. It runs against a
local SQLite file out of the box.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Database`
- No external database: the default connection string is `sqlite://squidstd.db`, and the schema is auto-migrated on
  startup.

## Steps

### 1. Register the database service

`RegisterDatabase` wires the `database` config section, the `IDatabaseService` (which owns the FreeSql instance), and
the open-generic `IDataAccess<>`. Starting the bootstrap starts the database service and auto-creates the schema.

[!code-csharp[](../../samples/SquidStd.Samples.Database/Program.cs#step-1)]

### 2. Insert and page over an entity

Resolve `IDataAccess<Product>` for any `BaseEntity`-derived type. `InsertAsync` assigns the id and timestamps;
`GetPagedAsync` returns a `PagedResultData<T>` with items and paging metadata.

[!code-csharp[](../../samples/SquidStd.Samples.Database/Program.cs#step-2)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Database
```

Prints the two inserted products ordered by price, with the total count and page metadata.

## How it works

`IDatabaseService` builds a single FreeSql ORM from the URI-style connection string (the scheme selects the provider:
`sqlite`, `postgres`, `mysql`, `sqlserver`). `IDataAccess<TEntity>` is the generic repository over any
`BaseEntity`: it exposes CRUD, bulk operations, a composable `Query()` (FreeSql `ISelect`), and `GetPagedAsync` for
page + total-count reads. Every entity gets a `Guid` id plus UTC `Created`/`Updated` timestamps from `BaseEntity`.

## See also

- [SquidStd.Database reference](../articles/database.md)
- [SquidStd.Database.Abstractions reference](../articles/database-abstractions.md)
