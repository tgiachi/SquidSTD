using FreeSql;
using SquidStd.Abstractions.Interfaces.Services;

namespace SquidStd.Database.Services;

/// <summary>
/// Owns the application's singleton FreeSql instance and its lifecycle.
/// </summary>
public interface IDatabaseService : ISquidStdService
{
    /// <summary>Gets the underlying FreeSql ORM instance.</summary>
    IFreeSql Orm { get; }
}
