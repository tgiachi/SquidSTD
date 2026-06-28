using System.Collections.Concurrent;
using Cronos;
using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Data.Scheduling;
using SquidStd.Core.Interfaces.Jobs;
using SquidStd.Core.Interfaces.Scheduling;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Services.Core.Services.Internal;

namespace SquidStd.Services.Core.Services.Scheduling;

/// <summary>
///     Cron scheduler built on the timer wheel: each job is a one-shot, self-rescheduling
///     timer. On fire, the handler is dispatched through <see cref="IJobSystem" />; an occurrence
///     is skipped when the previous run of the same job is still in flight.
/// </summary>
public sealed class CronSchedulerService : ICronScheduler, ISquidStdService, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, CronJobEntry> _entries = new(StringComparer.Ordinal);
    private readonly IJobSystem _jobs;
    private readonly ILogger _logger = Log.ForContext<CronSchedulerService>();
    private readonly ITimerService _timer;
    private int _disposed;

    /// <inheritdoc />
    public IReadOnlyCollection<CronJobInfo> Jobs
        => _entries.Values
            .Select(entry => new CronJobInfo
                {
                    JobId = entry.JobId,
                    Name = entry.Name,
                    CronExpression = entry.CronText,
                    NextOccurrenceUtc = entry.NextOccurrenceUtc,
                    IsRunning = Volatile.Read(ref entry.Running) == 1,
                    LastRunUtc = entry.LastRunUtc,
                    RunCount = Interlocked.Read(ref entry.RunCount)
                }
            )
            .ToArray();

    public CronSchedulerService(ITimerService timer, IJobSystem jobs)
    {
        _timer = timer;
        _jobs = jobs;
    }

    /// <inheritdoc />
    public string Schedule(string name, string cronExpression, Func<CancellationToken, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentNullException.ThrowIfNull(handler);

        var expression = CronExpression.Parse(cronExpression);

        var entry = new CronJobEntry
        {
            JobId = Guid.NewGuid().ToString("N"),
            Name = name,
            CronText = cronExpression,
            Expression = expression,
            Handler = handler
        };

        _entries[entry.JobId] = entry;
        ScheduleNext(entry);

        return entry.JobId;
    }

    /// <inheritdoc />
    public bool Unschedule(string jobId)
    {
        if (!_entries.TryRemove(jobId, out var entry))
        {
            return false;
        }

        if (entry.TimerId is not null)
        {
            _timer.UnregisterTimer(entry.TimerId);
        }

        return true;
    }

    /// <inheritdoc />
    public int UnscheduleByName(string name)
    {
        var ids = _entries.Values.Where(entry => entry.Name == name).Select(entry => entry.JobId).ToArray();
        var removed = 0;

        foreach (var id in ids)
        {
            if (Unschedule(id))
            {
                removed++;
            }
        }

        return removed;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        _cts.Cancel();

        foreach (var entry in _entries.Values)
        {
            if (entry.TimerId is not null)
            {
                _timer.UnregisterTimer(entry.TimerId);
            }
        }

        _entries.Clear();

        return ValueTask.CompletedTask;
    }

    private void OnTimer(CronJobEntry entry)
    {
        if (!_entries.ContainsKey(entry.JobId))
        {
            return;
        }

        if (Interlocked.CompareExchange(ref entry.Running, 1, 0) != 0)
        {
            _logger.Warning(
                "Cron job '{Name}' ({JobId}) skipped: previous run still in progress",
                entry.Name,
                entry.JobId
            );
        }
        else
        {
            _ = _jobs.ScheduleAsync(() => RunJob(entry));
        }

        ScheduleNext(entry);
    }

    private void RunJob(CronJobEntry entry)
    {
        try
        {
            entry.Handler(_cts.Token).GetAwaiter().GetResult();
            entry.LastRunUtc = DateTime.UtcNow;
            Interlocked.Increment(ref entry.RunCount);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Cron job '{Name}' ({JobId}) failed", entry.Name, entry.JobId);
        }
        finally
        {
            Interlocked.Exchange(ref entry.Running, 0);
        }
    }

    private void ScheduleNext(CronJobEntry entry)
    {
        var now = DateTime.UtcNow;
        var next = entry.Expression.GetNextOccurrence(now);

        if (next is null)
        {
            _logger.Information("Cron job '{Name}' ({JobId}) has no further occurrences", entry.Name, entry.JobId);
            entry.TimerId = null;
            entry.NextOccurrenceUtc = null;

            return;
        }

        var delay = next.Value - now;

        if (delay <= TimeSpan.Zero)
        {
            delay = TimeSpan.FromMilliseconds(1);
        }

        entry.NextOccurrenceUtc = next.Value;
        entry.TimerId = _timer.RegisterTimer(entry.Name, delay, () => OnTimer(entry));
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
