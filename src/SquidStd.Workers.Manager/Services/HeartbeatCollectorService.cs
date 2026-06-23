using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Workers.Abstractions;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Manager.Data.Config;

namespace SquidStd.Workers.Manager.Services;

/// <summary>
/// Subscribes to the heartbeat topic and folds each <see cref="WorkerHeartbeat" /> into the registry,
/// publishing any resulting status transition on the event bus.
/// </summary>
public sealed class HeartbeatCollectorService : ISquidStdService
{
    private readonly ILogger _logger = Log.ForContext<HeartbeatCollectorService>();
    private readonly IMessageTopic _topic;
    private readonly WorkerRegistry _registry;
    private readonly IEventBus _eventBus;
    private readonly string _topicName;
    private IDisposable? _subscription;

    public HeartbeatCollectorService(IMessageTopic topic, WorkerRegistry registry, IEventBus eventBus, WorkerManagerConfig config)
    {
        _topic = topic;
        _registry = registry;
        _eventBus = eventBus;
        _topicName = string.IsNullOrWhiteSpace(config.HeartbeatTopic) ? WorkerChannels.HeartbeatTopic : config.HeartbeatTopic;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _subscription = _topic.Subscribe<WorkerHeartbeat>(_topicName, OnHeartbeatAsync);
        _logger.Information("Worker manager collecting heartbeats from topic '{Topic}'.", _topicName);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        _subscription?.Dispose();
        _subscription = null;

        return ValueTask.CompletedTask;
    }

    private async Task OnHeartbeatAsync(WorkerHeartbeat heartbeat, CancellationToken cancellationToken)
    {
        try
        {
            var change = _registry.Record(heartbeat);
            if (change is not null)
            {
                await _eventBus.PublishAsync(change, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to process heartbeat from '{WorkerId}'.", heartbeat.WorkerId);
        }
    }
}
