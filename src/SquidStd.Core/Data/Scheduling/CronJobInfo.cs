namespace SquidStd.Core.Data.Scheduling;

/// <summary>
/// Immutable snapshot describing a registered cron job.
/// </summary>
public sealed class CronJobInfo
{
    /// <summary>Gets the unique job id.</summary>
    public required string JobId { get; init; }

    /// <summary>Gets the logical job name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the raw cron expression.</summary>
    public required string CronExpression { get; init; }

    /// <summary>Gets the next planned occurrence in UTC, or <c>null</c> when none.</summary>
    public DateTime? NextOccurrenceUtc { get; init; }

    /// <summary>Gets a value indicating whether a run is currently in progress.</summary>
    public bool IsRunning { get; init; }

    /// <summary>Gets the UTC time of the last successful run, or <c>null</c>.</summary>
    public DateTime? LastRunUtc { get; init; }

    /// <summary>Gets the number of successful runs so far.</summary>
    public long RunCount { get; init; }
}
