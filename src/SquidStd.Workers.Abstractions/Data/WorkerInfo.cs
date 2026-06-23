using SquidStd.Workers.Abstractions.Types;

namespace SquidStd.Workers.Abstractions.Data;

/// <summary>
/// The manager-side view of a worker, folded from incoming heartbeats.
/// </summary>
/// <param name="WorkerId">Stable identity of the worker.</param>
/// <param name="Status">Last known status; the manager sets <see cref="WorkerStatusType.Offline" /> on missed heartbeats.</param>
/// <param name="ActiveJobs">Jobs in progress as of the most recent heartbeat.</param>
/// <param name="MaxConcurrency">Maximum number of jobs the worker runs at once.</param>
/// <param name="FirstSeenUtc">UTC time the manager first saw this worker.</param>
/// <param name="LastSeenUtc">UTC time of the most recent heartbeat.</param>
public sealed record WorkerInfo(
    string WorkerId,
    WorkerStatusType Status,
    int ActiveJobs,
    int MaxConcurrency,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc);
