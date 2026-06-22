namespace SquidStd.Database.Abstractions.Types.Data;

/// <summary>
/// Supported database providers.
/// </summary>
public enum DatabaseProviderType
{
    /// <summary>SQLite.</summary>
    Sqlite,

    /// <summary>PostgreSQL.</summary>
    Postgres,

    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>MySQL.</summary>
    MySql
}
