<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Database.Abstractions</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Database.Abstractions/"><img src="https://img.shields.io/nuget/v/SquidStd.Database.Abstractions.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Database.Abstractions.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/database-abstractions.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Provider-agnostic data-access contracts for SquidStd. Entities derive from `BaseEntity` (a `Guid` id
plus UTC timestamps) and are accessed through the generic `IDataAccess<TEntity>` — full CRUD, bulk
operations, paging, and composable queries — without binding to any specific ORM.

## Install

```bash
dotnet add package SquidStd.Database.Abstractions
```

## Features

- `IDataAccess<TEntity>` — `InsertAsync`, `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`, `CountAsync`,
  `ExistsAsync`, bulk insert/update/delete, `QueryAsync`, and `GetPagedAsync`.
- `BaseEntity` — `Guid Id` plus `DateTimeOffset Created` / `Updated` (UTC), set by the data layer.
- `PagedResultData<T>` — items + `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasNext`, `HasPrevious`.
- `DatabaseConfig` — URI connection string + `AutoMigrate` flag.
- `DatabaseProviderType` — `Sqlite`, `Postgres`, `SqlServer`, `MySql`.

## Usage

```csharp
using SquidStd.Database.Abstractions.Data.Entities;
using SquidStd.Database.Abstractions.Interfaces.Data;

public sealed class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

// IDataAccess<User> is resolved from DI (see the SquidStd.Database package).
public async Task ExampleAsync(IDataAccess<User> users)
{
    await users.InsertAsync(new User { Name = "Ann" });
    var page = await users.GetPagedAsync(page: 1, pageSize: 20, orderBy: u => u.Name);
}
```

## Key types

| Type | Purpose |
|------|---------|
| `IDataAccess<TEntity>` | CRUD + bulk + paged + composable query contract. |
| `BaseEntity` | Guid id + UTC created/updated base entity. |
| `PagedResultData<T>` | Paginated result with metadata. |
| `DatabaseConfig` | Connection string + auto-migrate config section. |
| `DatabaseProviderType` | Supported provider enum. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
