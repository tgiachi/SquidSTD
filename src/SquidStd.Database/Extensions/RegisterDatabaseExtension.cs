using DryIoc;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Database.Abstractions.Data.Database;
using SquidStd.Database.Abstractions.Interfaces.Data;
using SquidStd.Database.Data;
using SquidStd.Database.Services;

namespace SquidStd.Database.Extensions;

/// <summary>
/// DI registration for the database subsystem.
/// </summary>
public static class RegisterDatabaseExtension
{
    /// <summary>
    /// Registers the database config section, the database service, and the open-generic data access.
    /// </summary>
    /// <param name="container">The DI container.</param>
    /// <returns>The same container for chaining.</returns>
    public static IContainer RegisterDatabase(this IContainer container)
    {
        container.RegisterConfigSection<DatabaseConfig>("database");
        container.RegisterStdService<IDatabaseService, DatabaseService>();
        container.Register(typeof(IDataAccess<>), typeof(FreeSqlDataAccess<>), Reuse.Transient);

        return container;
    }
}
