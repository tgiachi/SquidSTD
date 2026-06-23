using SquidStd.Workers.Abstractions.Types;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Interfaces;

namespace SquidStd.Workers.Services;

/// <summary>
/// Default <see cref="IWorkerState" /> backed by an interlocked counter; resolves identity and
/// concurrency from <see cref="WorkersConfig" /> at construction.
/// </summary>
public sealed class WorkerState : IWorkerState
{
    private int _activeJobs;

    /// <inheritdoc />
    public string WorkerId { get; }

    /// <inheritdoc />
    public int MaxConcurrency { get; }

    public WorkerState(WorkersConfig config)
    {
        WorkerId = string.IsNullOrWhiteSpace(config.WorkerId) ? Environment.MachineName : config.WorkerId;
        MaxConcurrency = config.MaxConcurrency > 0 ? config.MaxConcurrency : Environment.ProcessorCount;
    }

    /// <inheritdoc />
    public int ActiveJobs => Volatile.Read(ref _activeJobs);

    /// <inheritdoc />
    public WorkerStatusType Status => ActiveJobs == 0 ? WorkerStatusType.Idle : WorkerStatusType.Busy;

    /// <inheritdoc />
    public void JobStarted()
        => Interlocked.Increment(ref _activeJobs);

    /// <inheritdoc />
    public void JobFinished()
        => Interlocked.Decrement(ref _activeJobs);
}
