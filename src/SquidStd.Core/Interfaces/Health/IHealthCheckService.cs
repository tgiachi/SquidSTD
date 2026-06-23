using SquidStd.Core.Data.Health;

namespace SquidStd.Core.Interfaces.Health;

/// <summary>
/// Runs every registered <see cref="IHealthCheck" /> and aggregates the results.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>Runs all checks and returns the aggregated report.</summary>
    ValueTask<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);
}
