using SquidStd.Core.Interfaces.Config;

namespace SquidStd.Database.Abstractions.Data.Database;

/// <summary>
/// Database connection configuration.
/// </summary>
public sealed class DatabaseConfig : IConfigEntry
{
    /// <summary>
    /// Gets or sets the URI-style connection string (e.g. "sqlite://squidstd.db",
    /// "postgres://user:pass@host:5432/db"). The scheme selects the provider.
    /// </summary>
    public string ConnectionString { get; set; } = "sqlite://squidstd.db";

    /// <summary>
    /// Gets or sets a value indicating whether the schema is auto-synchronized on startup.
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    string IConfigEntry.SectionName => "database";

    Type IConfigEntry.ConfigType => typeof(DatabaseConfig);

    object IConfigEntry.CreateDefault()
        => new DatabaseConfig();
}
