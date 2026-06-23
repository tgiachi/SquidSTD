namespace SquidStd.Workers.Exceptions;

/// <summary>
/// Thrown when a job arrives whose name has no registered <see cref="Interfaces.IJobHandler" />.
/// </summary>
public sealed class JobHandlerNotFoundException : Exception
{
    /// <summary>The job name that had no handler.</summary>
    public string JobName { get; }

    /// <summary>Initializes the exception for the given job name.</summary>
    public JobHandlerNotFoundException(string jobName)
        : base($"No job handler is registered for job '{jobName}'.")
    {
        JobName = jobName;
    }
}
