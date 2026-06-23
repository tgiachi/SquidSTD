using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Workers.Manager.Data.Config;

namespace SquidStd.Workers.Manager.Services;

/// <summary>
/// Periodically marks workers Offline whose heartbeats have stopped, using the timer wheel
/// (<see cref="ITimerService" />), and publishes the resulting transitions.
/// </summary>
public sealed class WorkerOfflineSweepService : ISquidStdService
{
    private const int DefaultIntervalSeconds = 10;

    private readonly ILogger _logger = Log.ForContext<WorkerOfflineSweepService>();
    private readonly ITimerService _timer;
    private readonly WorkerRegistry _registry;
    private readonly IEventBus _eventBus;
    private readonly TimeSpan _interval;
    private string? _timerId;

    public WorkerOfflineSweepService(ITimerService timer, WorkerRegistry registry, IEventBus eventBus, WorkerManagerConfig config)
    {
        _timer = timer;
        _registry = registry;
        _eventBus = eventBus;

        var seconds = config.SweepIntervalSeconds > 0 ? config.SweepIntervalSeconds : DefaultIntervalSeconds;
        if (config.SweepIntervalSeconds <= 0)
        {
            _logger.Warning(
                "SweepIntervalSeconds was {Value}; falling back to {Default}s.",
                config.SweepIntervalSeconds,
                DefaultIntervalSeconds);
        }

        _interval = TimeSpan.FromSeconds(seconds);
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _timerId = _timer.RegisterTimer("worker-offline-sweep", _interval, OnTick, delay: _interval, repeat: true);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_timerId is not null)
        {
            _timer.UnregisterTimer(_timerId);
            _timerId = null;
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>Runs one sweep and publishes the transitions. Public so it can be driven directly in tests.</summary>
    public async Task RunSweepAsync()
    {
        try
        {
            foreach (var change in _registry.Sweep(DateTime.UtcNow))
            {
                await _eventBus.PublishAsync(change, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Worker offline sweep failed.");
        }
    }

    private void OnTick()
        => _ = RunSweepAsync();
}
