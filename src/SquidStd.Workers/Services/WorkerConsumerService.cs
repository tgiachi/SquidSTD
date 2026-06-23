using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Workers.Abstractions;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Exceptions;
using SquidStd.Workers.Interfaces;

namespace SquidStd.Workers.Services;

/// <summary>
/// Subscribes to the jobs queue and dispatches each <see cref="JobRequest" /> to its handler,
/// bounded by <see cref="IWorkerState.MaxConcurrency" />.
/// </summary>
public sealed class WorkerConsumerService : ISquidStdService, IQueueMessageListenerAsync<JobRequest>
{
    private readonly ILogger _logger = Log.ForContext<WorkerConsumerService>();
    private readonly IMessageQueue _queue;
    private readonly IJobDispatcher _dispatcher;
    private readonly IWorkerState _state;
    private readonly SemaphoreSlim _semaphore;
    private readonly string _queueName;
    private IDisposable? _subscription;

    /// <summary>Constructs the consumer wired to the messaging queue and worker state.</summary>
    public WorkerConsumerService(IMessageQueue queue, IJobDispatcher dispatcher, IWorkerState state, WorkersConfig config)
    {
        _queue = queue;
        _dispatcher = dispatcher;
        _state = state;
        _semaphore = new SemaphoreSlim(state.MaxConcurrency);
        _queueName = string.IsNullOrWhiteSpace(config.JobQueue) ? WorkerChannels.JobQueue : config.JobQueue;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _subscription = _queue.Subscribe(_queueName, this);
        _logger.Information("Worker consuming jobs from queue '{Queue}'.", _queueName);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        _subscription?.Dispose();
        _subscription = null;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async Task HandleAsync(JobRequest message, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        _state.JobStarted();

        try
        {
            await _dispatcher.DispatchAsync(message, cancellationToken);
        }
        catch (JobHandlerNotFoundException)
        {
            _logger.Warning("No handler for job '{JobName}'; dropping.", message.JobName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Debug("Job '{JobName}' cancelled during shutdown; will be redelivered.", message.JobName);

            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Job '{JobName}' failed; it will be retried.", message.JobName);

            throw;
        }
        finally
        {
            _state.JobFinished();
            _semaphore.Release();
        }
    }
}
