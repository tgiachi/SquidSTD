using SquidStd.Workers.Abstractions.Types;

namespace SquidStd.Workers.Interfaces;

/// <summary>
/// Shared runtime state of a worker, read by the heartbeat service and mutated by the consumer.
/// </summary>
public interface IWorkerState
{
    /// <summary>Stable worker identity.</summary>
    string WorkerId { get; }

    /// <summary>Maximum jobs processed in parallel.</summary>
    int MaxConcurrency { get; }

    /// <summary>Jobs currently in progress.</summary>
    int ActiveJobs { get; }

    /// <summary><see cref="WorkerStatusType.Busy" /> while any job is active, otherwise <see cref="WorkerStatusType.Idle" />.</summary>
    WorkerStatusType Status { get; }

    /// <summary>Records that a job started (increments <see cref="ActiveJobs" />).</summary>
    void JobStarted();

    /// <summary>Records that a job finished (decrements <see cref="ActiveJobs" />).</summary>
    void JobFinished();
}
