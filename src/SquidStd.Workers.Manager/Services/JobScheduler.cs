using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Workers.Abstractions;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Interfaces;

namespace SquidStd.Workers.Manager.Services;

/// <summary>
/// Default <see cref="IJobScheduler" />: publishes <see cref="JobRequest" />s onto the configured jobs queue.
/// </summary>
public sealed class JobScheduler : IJobScheduler
{
    private readonly IMessageQueue _queue;
    private readonly string _queueName;

    public JobScheduler(IMessageQueue queue, WorkerManagerConfig config)
    {
        _queue = queue;
        _queueName = string.IsNullOrWhiteSpace(config.JobQueue) ? WorkerChannels.JobQueue : config.JobQueue;
    }

    /// <inheritdoc />
    public Task EnqueueAsync(string jobName, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default)
        => _queue.PublishAsync(_queueName, new JobRequest(jobName, parameters), cancellationToken);

    /// <inheritdoc />
    public Task EnqueueAsync(string jobName, CancellationToken cancellationToken = default)
        => EnqueueAsync(jobName, new Dictionary<string, string>(), cancellationToken);
}
