using SquidStd.Workers.Abstractions;

namespace SquidStd.Workers.Manager.Data.Config;

/// <summary>
/// Configuration for the worker manager (config section "workerManager").
/// </summary>
public sealed class WorkerManagerConfig
{
    /// <summary>Seconds without a heartbeat before a worker is considered Offline. Falls back to 30 when not positive.</summary>
    public int OfflineTimeoutSeconds { get; set; } = 30;

    /// <summary>Seconds between offline sweeps. Falls back to 10 when not positive.</summary>
    public int SweepIntervalSeconds { get; set; } = 10;

    /// <summary>Queue jobs are enqueued onto.</summary>
    public string JobQueue { get; set; } = WorkerChannels.JobQueue;

    /// <summary>Topic heartbeats are collected from.</summary>
    public string HeartbeatTopic { get; set; } = WorkerChannels.HeartbeatTopic;
}
