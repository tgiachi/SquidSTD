using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Mail.Abstractions.Data.Config;
using SquidStd.Mail.Abstractions.Data.Events;
using SquidStd.Mail.Abstractions.Interfaces;

namespace SquidStd.Mail.MailKit.Services;

/// <summary>Polls the mailbox on the timer wheel and publishes a <see cref="MailReceivedEvent" /> per new message.</summary>
public sealed class MailPollingService : ISquidStdService, IDisposable
{
    private const int DefaultIntervalSeconds = 60;
    private readonly IEventBus _eventBus;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _interval;

    private readonly ILogger _logger = Log.ForContext<MailPollingService>();
    private readonly IMailReader _reader;
    private readonly ITimerService _timer;
    private string? _timerId;

    public MailPollingService(IMailReader reader, IEventBus eventBus, ITimerService timer, MailOptions options)
    {
        _reader = reader;
        _eventBus = eventBus;
        _timer = timer;

        var seconds = options.PollIntervalSeconds > 0 ? options.PollIntervalSeconds : DefaultIntervalSeconds;

        if (options.PollIntervalSeconds <= 0)
        {
            _logger.Warning(
                "PollIntervalSeconds was {Value}; falling back to {Default}s.",
                options.PollIntervalSeconds,
                DefaultIntervalSeconds
            );
        }

        _interval = TimeSpan.FromSeconds(seconds);
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _timerId = _timer.RegisterTimer("mail-poll", _interval, OnTick, _interval, true);

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

    /// <summary>Runs one poll and publishes events. Public so tests can drive it without the timer wheel.</summary>
    public async Task PollOnceAsync()
    {
        if (!await _gate.WaitAsync(0))
        {
            return;
        }

        try
        {
            var messages = await _reader.FetchNewAsync();

            foreach (var message in messages)
            {
                await _eventBus.PublishAsync(new MailReceivedEvent(message), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Mail poll failed.");
        }
        finally
        {
            _gate.Release();
        }
    }

    private void OnTick()
    {
        _ = PollOnceAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Dispose();
    }
}
