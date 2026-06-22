namespace SquidStd.Core.Interfaces.Jobs;

/// <summary>
/// Schedules work on a fixed-size pool of worker threads.
/// </summary>
public interface IJobSystem : IDisposable
{
    /// <summary>
    /// Gets the number of worker threads.
    /// </summary>
    int WorkerCount { get; }

    /// <summary>
    /// Gets the number of jobs waiting in the queue.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// Gets the number of jobs currently executing.
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    /// Gets the number of jobs that completed since startup.
    /// </summary>
    long CompletedCount { get; }

    /// <summary>
    /// Schedules work on a worker thread.
    /// </summary>
    /// <param name="work">Work invoked on a worker thread.</param>
    /// <param name="cancellationToken">Token used to cancel the job before it starts.</param>
    /// <returns>A task that completes when the job finishes.</returns>
    Task ScheduleAsync(Action work, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules work on a worker thread and returns the result.
    /// </summary>
    /// <param name="work">Work invoked on a worker thread.</param>
    /// <param name="cancellationToken">Token used to cancel the job before it starts.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>A task that completes with the job result.</returns>
    Task<T> ScheduleAsync<T>(Func<T> work, CancellationToken cancellationToken = default);
}
