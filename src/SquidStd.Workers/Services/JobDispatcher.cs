using Serilog;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Exceptions;
using SquidStd.Workers.Interfaces;

namespace SquidStd.Workers.Services;

/// <summary>
/// Default <see cref="IJobDispatcher" />: indexes the registered handlers by job name.
/// </summary>
public sealed class JobDispatcher : IJobDispatcher
{
    private readonly ILogger _logger = Log.ForContext<JobDispatcher>();
    private readonly Dictionary<string, IJobHandler> _handlers = new(StringComparer.Ordinal);

    public JobDispatcher(IEnumerable<IJobHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            if (_handlers.ContainsKey(handler.JobName))
            {
                _logger.Warning(
                    "Duplicate job handler for '{JobName}'; the last registration wins.",
                    handler.JobName);
            }

            _handlers[handler.JobName] = handler;
        }
    }

    /// <inheritdoc />
    public Task DispatchAsync(JobRequest job, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(job.JobName, out var handler))
        {
            throw new JobHandlerNotFoundException(job.JobName);
        }

        return handler.HandleAsync(job, cancellationToken);
    }
}
