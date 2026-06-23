using SquidStd.Core.Types.Health;

namespace SquidStd.Core.Data.Health;

/// <summary>
/// Result of a single health check. <see cref="Duration" /> is stamped by the aggregator.
/// </summary>
public sealed record HealthCheckResult
{
    /// <summary>Health status of the check.</summary>
    public required HealthStatus Status { get; init; }

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional exception captured when the check failed.</summary>
    public Exception? Exception { get; init; }

    /// <summary>How long the check took.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Creates a healthy result.</summary>
    public static HealthCheckResult Healthy(string? description = null)
        => new() { Status = HealthStatus.Healthy, Description = description };

    /// <summary>Creates an unhealthy result.</summary>
    public static HealthCheckResult Unhealthy(string? description = null, Exception? exception = null)
        => new() { Status = HealthStatus.Unhealthy, Description = description, Exception = exception };
}
