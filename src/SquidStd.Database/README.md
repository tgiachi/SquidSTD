<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Database</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Database/"><img src="https://img.shields.io/nuget/v/SquidStd.Database.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Database.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/database.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

FreeSql-backed implementation of the SquidStd.Database.Abstractions contracts. It owns a singleton
`IFreeSql`, exposes a generic `FreeSqlDataAccess<TEntity>` with transactional writes (rollback on
failure), bulk operations and paging, parses URI-style connection strings, and can auto-sync the schema
on startup.

## Install

```bash
dotnet add package SquidStd.Database
```

## Features

- One-line registration: `container.RegisterDatabase()` (config section + service + open-generic `IDataAccess<>`).
- Providers via URI scheme: `sqlite://`, `postgres://`, `sqlserver://`, `mysql://`.
- `FreeSqlDataAccess<TEntity>` — CRUD, bulk insert/update/delete, `QueryAsync`, `GetPagedAsync`; writes
  run inside a unit of work and roll back on error. Sets `Id`/`Created`/`Updated` automatically.
- Optional `AutoMigrate` (FreeSql `AutoSyncStructure`) to create/update tables on startup.
- ZLinq in-memory helpers for zero-allocation post-processing of materialized results.

## Usage

```csharp
using DryIoc;
using SquidStd.Database.Abstractions.Interfaces.Data;
using SquidStd.Database.Extensions;

var container = new Container();
container.RegisterDatabase(); // reads the "database" config section

// DatabaseConfig: ConnectionString = "postgres://user:pass@host:5432/app", AutoMigrate = true
var users = container.Resolve<IDataAccess<User>>();
await users.InsertAsync(new User { Name = "Ann" });
var page = await users.GetPagedAsync(page: 1, pageSize: 20, orderBy: u => u.Name);
```

## Key types

| Type | Purpose |
|------|---------|
| `RegisterDatabaseExtension` | `RegisterDatabase()` DI registration. |
| `DatabaseService` | Owns the singleton `IFreeSql`; builds it and (optionally) migrates. |
| `FreeSqlDataAccess<TEntity>` | FreeSql `IDataAccess<TEntity>` implementation. |
| `ConnectionStringParser` | URI → provider + native connection string. |
| `ZLinqResultExtensions` | Zero-alloc in-memory result helpers. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
