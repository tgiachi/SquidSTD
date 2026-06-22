using SquidStd.Core.Data.Scheduling;

namespace SquidStd.Core.Interfaces.Scheduling;

/// <summary>
/// Schedules asynchronous jobs on standard 5-field cron expressions (UTC).
/// </summary>
public interface ICronScheduler
{
    /// <summary>
    /// Registers a cron job.
    /// </summary>
    /// <param name="name">Logical job name.</param>
    /// <param name="cronExpression">Standard 5-field cron expression, evaluated in UTC.</param>
    /// <param name="handler">Asynchronous work invoked on each occurrence.</param>
    /// <returns>The unique job id.</returns>
    string Schedule(string name, string cronExpression, Func<CancellationToken, Task> handler);

    /// <summary>Removes a job by id.</summary>
    /// <param name="jobId">The job id returned by <see cref="Schedule" />.</param>
    /// <returns><c>true</c> when a job was removed; otherwise <c>false</c>.</returns>
    bool Unschedule(string jobId);

    /// <summary>Removes all jobs with the given name.</summary>
    /// <param name="name">The job name.</param>
    /// <returns>The number of removed jobs.</returns>
    int UnscheduleByName(string name);

    /// <summary>Gets a snapshot of all registered jobs.</summary>
    IReadOnlyCollection<CronJobInfo> Jobs { get; }
}
