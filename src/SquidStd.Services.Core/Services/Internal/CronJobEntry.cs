using Cronos;

namespace SquidStd.Services.Core.Services.Internal;

/// <summary>
/// Internal mutable state for a registered cron job.
/// </summary>
internal sealed class CronJobEntry
{
    public required string JobId { get; init; }
    public required string Name { get; init; }
    public required string CronText { get; init; }
    public required CronExpression Expression { get; init; }
    public required Func<CancellationToken, Task> Handler { get; init; }

    public string? TimerId;
    public DateTime? NextOccurrenceUtc;
    public DateTime? LastRunUtc;
    public int Running;
    public long RunCount;
}
