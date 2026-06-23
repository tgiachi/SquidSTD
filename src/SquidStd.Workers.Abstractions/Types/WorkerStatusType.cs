namespace SquidStd.Workers.Abstractions.Types;

/// <summary>
/// Lifecycle status of a worker. Workers report <see cref="Idle" /> or <see cref="Busy" />;
/// the manager assigns <see cref="Offline" /> when a worker's heartbeats stop arriving.
/// </summary>
public enum WorkerStatusType
{
    /// <summary>The worker is running and has no job in progress.</summary>
    Idle,

    /// <summary>The worker is currently processing a job.</summary>
    Busy,

    /// <summary>The manager has not seen a heartbeat within the expected window.</summary>
    Offline
}
