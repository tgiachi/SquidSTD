using SquidStd.Workers.Abstractions.Types;

namespace SquidStd.Workers.Abstractions.Data;

/// <summary>
/// The manager-side view of a worker, folded from incoming heartbeats.
/// </summary>
/// <param name="WorkerId">Stable identity of the worker.</param>
/// <param name="Status">Last known status; the manager sets <see cref="WorkerStatusType.Offline" /> on missed heartbeats.</param>
/// <param name="FirstSeenUtc">UTC time the manager first saw this worker.</param>
/// <param name="LastSeenUtc">UTC time of the most recent heartbeat.</param>
/// <param name="CurrentJob">Name of the job last reported in progress, or <c>null</c>.</param>
public sealed record WorkerInfo(
    string WorkerId,
    WorkerStatusType Status,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc,
    string? CurrentJob);
