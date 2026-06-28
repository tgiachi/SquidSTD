using System.Diagnostics;
using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Data.Timing;
using SquidStd.Core.Interfaces.Timing;

namespace SquidStd.Services.Core.Services.Scheduling;

/// <summary>
///     Periodically advances the timer wheel so that wheel-backed timers fire in a normal
///     (non-game-loop) application. Drives <see cref="ITimerService.UpdateTicksDelta" /> on a
///     background loop.
/// </summary>
public sealed class TimerWheelPumpService : ISquidStdService, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger _logger = Log.ForContext<TimerWheelPumpService>();
    private readonly TimeSpan _pumpInterval;
    private readonly ITimerService _timer;
    private int _disposed;
    private Task? _loop;

    public TimerWheelPumpService(ITimerService timer, TimerWheelPumpConfig config)
    {
        if (config.PumpInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(config), "PumpInterval must be positive.");
        }

        _timer = timer;
        _pumpInterval = config.PumpInterval;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _loop = Task.Run(() => PumpLoopAsync(_cts.Token), CancellationToken.None);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_loop is null)
        {
            return;
        }

        await _cts.CancelAsync();

        try
        {
            await _loop;
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }

        _loop = null;
    }

    private async Task PumpLoopAsync(CancellationToken token)
    {
        var clock = Stopwatch.StartNew();
        using var ticker = new PeriodicTimer(_pumpInterval);

        try
        {
            while (await ticker.WaitForNextTickAsync(token))
            {
                try
                {
                    _timer.UpdateTicksDelta(clock.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Timer wheel pump tick failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _cts.Cancel();
        _cts.Dispose();
    }
}
