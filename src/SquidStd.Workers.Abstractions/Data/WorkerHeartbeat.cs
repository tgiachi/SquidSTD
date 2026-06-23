using SquidStd.Workers.Abstractions.Types;

namespace SquidStd.Workers.Abstractions.Data;

/// <summary>
/// A liveness signal a worker publishes on the heartbeat topic at a fixed interval.
/// </summary>
/// <param name="WorkerId">Stable identity of the reporting worker.</param>
/// <param name="TimestampUtc">UTC time the heartbeat was produced.</param>
/// <param name="Status">Self-reported status (<see cref="WorkerStatusType.Idle" /> or <see cref="WorkerStatusType.Busy" />).</param>
/// <param name="CurrentJob">Name of the job in progress, or <c>null</c> when idle.</param>
public sealed record WorkerHeartbeat(
    string WorkerId,
    DateTime TimestampUtc,
    WorkerStatusType Status,
    string? CurrentJob);
