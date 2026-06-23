using SquidStd.Workers.Abstractions.Types;

namespace SquidStd.Workers.Abstractions.Data;

/// <summary>
/// A liveness signal a worker publishes on the heartbeat topic at a fixed interval.
/// </summary>
/// <param name="WorkerId">Stable identity of the reporting worker.</param>
/// <param name="TimestampUtc">UTC time the heartbeat was produced.</param>
/// <param name="Status">Self-reported status (<see cref="WorkerStatusType.Idle" /> when no job is running, else <see cref="WorkerStatusType.Busy" />).</param>
/// <param name="ActiveJobs">Number of jobs currently in progress on this worker.</param>
/// <param name="MaxConcurrency">Maximum number of jobs the worker runs at once.</param>
public sealed record WorkerHeartbeat(
    string WorkerId,
    DateTime TimestampUtc,
    WorkerStatusType Status,
    int ActiveJobs,
    int MaxConcurrency);
