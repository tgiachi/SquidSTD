namespace SquidStd.Core.Data.Config;

/// <summary>
/// Options for the health-check aggregator.
/// </summary>
public sealed class HealthCheckOptions
{
    /// <summary>Per-check timeout. A check exceeding it is reported unhealthy. Default 5 seconds.</summary>
    public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
