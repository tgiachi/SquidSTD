using FreeSql;
using Serilog;
using SquidStd.Database.Abstractions.Data.Database;
using SquidStd.Database.Abstractions.Types.Data;
using SquidStd.Database.Connection;
using SquidStd.Database.Interfaces.Services;

namespace SquidStd.Database.Services;

/// <summary>
/// Builds and owns the singleton FreeSql instance, logging SQL and migrations verbosely.
/// </summary>
public sealed class DatabaseService : IDatabaseService
{
    private static readonly ILogger Logger = Log.ForContext<DatabaseService>();

    private readonly DatabaseConfig _config;
    private IFreeSql? _orm;
    private int _started;

    /// <inheritdoc />
    public IFreeSql Orm
        => _orm ?? throw new InvalidOperationException("Database service is not started.");

    /// <summary>
    /// Initializes the database service.
    /// </summary>
    /// <param name="config">The database configuration section.</param>
    public DatabaseService(DatabaseConfig config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        var parsed = ConnectionStringParser.Parse(_config.ConnectionString);
        Logger.Verbose("Building FreeSql for provider {Provider}", parsed.Provider);

        var builder = new FreeSqlBuilder()
            .UseConnectionString(MapDataType(parsed.Provider), parsed.NativeConnectionString)
            .UseAutoSyncStructure(_config.AutoMigrate)
            .UseMonitorCommand(cmd => Logger.Verbose("SQL {Sql}", cmd.CommandText));

        _orm = builder.Build();
        _orm.Aop.SyncStructureAfter += (_, e) =>
            Logger.Verbose("Migrated {Entities} -> {Sql}", e.EntityTypes, e.Sql);

        Logger.Information(
            "Database service started ({Provider}, autoMigrate={AutoMigrate})",
            parsed.Provider,
            _config.AutoMigrate);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _orm?.Dispose();
        _orm = null;

        return ValueTask.CompletedTask;
    }

    private static DataType MapDataType(DatabaseProviderType provider)
        => provider switch
        {
            DatabaseProviderType.Sqlite => DataType.Sqlite,
            DatabaseProviderType.Postgres => DataType.PostgreSQL,
            DatabaseProviderType.SqlServer => DataType.SqlServer,
            DatabaseProviderType.MySql => DataType.MySql,
            _ => throw new NotSupportedException($"Unsupported provider {provider}.")
        };
}
