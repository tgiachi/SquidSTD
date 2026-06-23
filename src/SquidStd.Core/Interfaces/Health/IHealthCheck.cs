using SquidStd.Core.Data.Health;

namespace SquidStd.Core.Interfaces.Health;

/// <summary>
/// A single health check for one component.
/// </summary>
public interface IHealthCheck
{
    /// <summary>Logical check name (used as the report entry key).</summary>
    string Name { get; }

    /// <summary>Runs the check.</summary>
    /// <param name="cancellationToken">Token used to cancel the check (also fires on per-check timeout).</param>
    ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
