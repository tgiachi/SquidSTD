using SquidStd.Workers.Abstractions;

namespace SquidStd.Workers.Data.Config;

/// <summary>
/// Configuration for the worker runtime (config section "workers").
/// </summary>
public sealed class WorkersConfig
{
    /// <summary>
    /// Stable worker identity. When blank, the runtime falls back to the machine name
    /// (in Docker, the container hostname).
    /// </summary>
    public string WorkerId { get; set; } = string.Empty;

    /// <summary>Seconds between heartbeats. Falls back to 10 when not positive.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 10;

    /// <summary>Maximum jobs processed in parallel. Falls back to the processor count when not positive.</summary>
    public int MaxConcurrency { get; set; }

    /// <summary>Queue the worker consumes jobs from.</summary>
    public string JobQueue { get; set; } = WorkerChannels.JobQueue;

    /// <summary>Topic the worker publishes heartbeats to.</summary>
    public string HeartbeatTopic { get; set; } = WorkerChannels.HeartbeatTopic;
}
