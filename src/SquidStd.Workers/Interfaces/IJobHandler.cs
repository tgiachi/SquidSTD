using SquidStd.Workers.Abstractions.Data;

namespace SquidStd.Workers.Interfaces;

/// <summary>
/// Handles jobs of a single named kind. Implemented by consumers of the worker library.
/// </summary>
public interface IJobHandler
{
    /// <summary>The <see cref="JobRequest.JobName" /> this handler processes.</summary>
    string JobName { get; }

    /// <summary>Executes the job.</summary>
    Task HandleAsync(JobRequest job, CancellationToken cancellationToken);
}
