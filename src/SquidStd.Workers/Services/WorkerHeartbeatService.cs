using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Workers.Abstractions;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Interfaces;

namespace SquidStd.Workers.Services;

/// <summary>
/// Publishes a <see cref="WorkerHeartbeat" /> on the heartbeat topic immediately on start and then once
/// per configured interval.
/// </summary>
public sealed class WorkerHeartbeatService : ISquidStdService
{
    private const int DefaultIntervalSeconds = 10;

    private readonly ILogger _logger = Log.ForContext<WorkerHeartbeatService>();
    private readonly IMessageTopic _topic;
    private readonly IWorkerState _state;
    private readonly TimeSpan _interval;
    private readonly string _topicName;
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    public WorkerHeartbeatService(IMessageTopic topic, IWorkerState state, WorkersConfig config)
    {
        _topic = topic;
        _state = state;

        var seconds = config.HeartbeatIntervalSeconds > 0 ? config.HeartbeatIntervalSeconds : DefaultIntervalSeconds;
        if (config.HeartbeatIntervalSeconds <= 0)
        {
            _logger.Warning(
                "HeartbeatIntervalSeconds was {Value}; falling back to {Default}s.",
                config.HeartbeatIntervalSeconds,
                DefaultIntervalSeconds);
        }

        _interval = TimeSpan.FromSeconds(seconds);
        _topicName = string.IsNullOrWhiteSpace(config.HeartbeatTopic) ? WorkerChannels.HeartbeatTopic : config.HeartbeatTopic;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunLoopAsync(_loopCts.Token), CancellationToken.None);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_loopCts is null)
        {
            return;
        }

        await _loopCts.CancelAsync();

        try
        {
            if (_loopTask is not null)
            {
                await _loopTask;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        finally
        {
            _loopCts.Dispose();
            _loopCts = null;
            _loopTask = null;
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval);

        await PublishAsync(cancellationToken);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await PublishAsync(cancellationToken);
        }
    }

    private async Task PublishAsync(CancellationToken cancellationToken)
    {
        try
        {
            var heartbeat = new WorkerHeartbeat(
                _state.WorkerId,
                DateTime.UtcNow,
                _state.Status,
                _state.ActiveJobs,
                _state.MaxConcurrency);

            await _topic.PublishAsync(_topicName, heartbeat, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to publish heartbeat; will retry on the next tick.");
        }
    }
}
