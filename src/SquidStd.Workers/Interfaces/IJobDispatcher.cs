using SquidStd.Workers.Abstractions.Data;

namespace SquidStd.Workers.Interfaces;

/// <summary>
/// Routes a <see cref="JobRequest" /> to the <see cref="IJobHandler" /> registered for its job name.
/// </summary>
public interface IJobDispatcher
{
    /// <summary>
    /// Dispatches the job to its handler.
    /// </summary>
    /// <exception cref="Exceptions.JobHandlerNotFoundException">No handler matches the job name.</exception>
    Task DispatchAsync(JobRequest job, CancellationToken cancellationToken);
}
