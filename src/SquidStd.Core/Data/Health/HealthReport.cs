using SquidStd.Core.Types.Health;

namespace SquidStd.Core.Data.Health;

/// <summary>
/// Aggregated result of running every registered health check.
/// </summary>
public sealed record HealthReport
{
    /// <summary>Overall status (unhealthy if any entry is unhealthy).</summary>
    public required HealthStatus Status { get; init; }

    /// <summary>Per-check results keyed by check name.</summary>
    public required IReadOnlyDictionary<string, HealthCheckResult> Entries { get; init; }

    /// <summary>Wall-clock time taken to run all checks.</summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>UTC timestamp when the report was produced.</summary>
    public DateTime TimestampUtc { get; init; }
}
