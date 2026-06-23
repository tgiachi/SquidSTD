namespace SquidStd.Workers.Manager.Interfaces;

/// <summary>
/// Enqueues jobs for workers to consume.
/// </summary>
public interface IJobScheduler
{
    /// <summary>Enqueues a job with parameters.</summary>
    Task EnqueueAsync(string jobName, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default);

    /// <summary>Enqueues a job with no parameters.</summary>
    Task EnqueueAsync(string jobName, CancellationToken cancellationToken = default);
}
