namespace SquidStd.Workers.Abstractions;

/// <summary>
/// Default names of the messaging channels the workers system uses. Shared so the manager
/// and workers agree out of the box; either side may override them via its own configuration.
/// </summary>
public static class WorkerChannels
{
    /// <summary>Competing-consumers queue the manager publishes <c>JobRequest</c>s onto.</summary>
    public const string JobQueue = "squidstd.workers.jobs";

    /// <summary>Fan-out topic workers publish <c>WorkerHeartbeat</c>s onto.</summary>
    public const string HeartbeatTopic = "squidstd.workers.heartbeat";
}
