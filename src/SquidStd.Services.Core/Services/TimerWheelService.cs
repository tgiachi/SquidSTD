using System.Diagnostics;
using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Data.Timing;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Services.Core.Services.Internal;

namespace SquidStd.Services.Core.Services;

/// <summary>
/// Hashed timer wheel driven by absolute timestamp updates.
/// </summary>
public sealed class TimerWheelService : ITimerService, ISquidStdService
{
    private readonly ILogger _logger = Log.ForContext<TimerWheelService>();
    private readonly Lock _syncRoot = new();
    private readonly TimeSpan _tickDuration;
    private readonly double _tickDurationMs;
    private readonly Dictionary<string, HashSet<string>> _timerIdsByName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TimerEntry> _timersById = new(StringComparer.Ordinal);
    private readonly LinkedList<TimerEntry>[] _wheel;
    private double _accumulatedMilliseconds;
    private long _currentTick;
    private long _lastTimestampMilliseconds = -1;

    /// <summary>
    /// Initializes the timer wheel service.
    /// </summary>
    /// <param name="config">Timer wheel configuration.</param>
    public TimerWheelService(TimerWheelConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.TickDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config),
                "TimerWheelConfig.TickDuration must be positive."
            );
        }

        if (config.WheelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config),
                "TimerWheelConfig.WheelSize must be positive."
            );
        }

        _tickDuration = config.TickDuration;
        _tickDurationMs = _tickDuration.TotalMilliseconds;
        _wheel = new LinkedList<TimerEntry>[config.WheelSize];

        for (var i = 0; i < _wheel.Length; i++)
        {
            _wheel[i] = new();
        }
    }

    /// <inheritdoc />
    public string RegisterTimer(
        string name,
        TimeSpan interval,
        Action callback,
        TimeSpan? delay = null,
        bool repeat = false
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Timer name cannot be empty.", nameof(name));
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");
        }

        ArgumentNullException.ThrowIfNull(callback);

        var dueTime = delay ?? interval;

        if (dueTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be positive.");
        }

        var entry = new TimerEntry
        {
            Callback = callback,
            Id = Guid.NewGuid().ToString("N"),
            Interval = interval,
            Name = name,
            Repeat = repeat
        };

        lock (_syncRoot)
        {
            _timersById[entry.Id] = entry;

            if (!_timerIdsByName.TryGetValue(name, out var timerIds))
            {
                timerIds = [];
                _timerIdsByName[name] = timerIds;
            }

            timerIds.Add(entry.Id);
            ScheduleEntry(entry, dueTime);
        }

        return entry.Id;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UnregisterAllTimers();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void UnregisterAllTimers()
    {
        lock (_syncRoot)
        {
            _timersById.Clear();
            _timerIdsByName.Clear();

            for (var i = 0; i < _wheel.Length; i++)
            {
                _wheel[i].Clear();
            }
        }
    }

    /// <inheritdoc />
    public bool UnregisterTimer(string timerId)
    {
        if (string.IsNullOrWhiteSpace(timerId))
        {
            return false;
        }

        lock (_syncRoot)
        {
            return RemoveEntryById(timerId);
        }
    }

    /// <inheritdoc />
    public int UnregisterTimersByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 0;
        }

        lock (_syncRoot)
        {
            if (!_timerIdsByName.TryGetValue(name, out var timerIds) || timerIds.Count == 0)
            {
                return 0;
            }

            var ids = timerIds.ToArray();
            var removed = 0;

            for (var i = 0; i < ids.Length; i++)
            {
                if (RemoveEntryById(ids[i]))
                {
                    removed++;
                }
            }

            return removed;
        }
    }

    /// <inheritdoc />
    public int UpdateTicksDelta(long timestampMilliseconds)
    {
        if (timestampMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timestampMilliseconds),
                "Timestamp must be non-negative."
            );
        }

        long ticksToProcess;

        lock (_syncRoot)
        {
            if (_lastTimestampMilliseconds < 0)
            {
                _lastTimestampMilliseconds = timestampMilliseconds;

                return 0;
            }

            var deltaMilliseconds = timestampMilliseconds - _lastTimestampMilliseconds;

            if (deltaMilliseconds <= 0)
            {
                return 0;
            }

            _lastTimestampMilliseconds = timestampMilliseconds;
            _accumulatedMilliseconds += deltaMilliseconds;
            ticksToProcess = (long)Math.Floor(_accumulatedMilliseconds / _tickDurationMs);

            if (ticksToProcess <= 0)
            {
                return 0;
            }

            _accumulatedMilliseconds -= ticksToProcess * _tickDurationMs;
        }

        for (var i = 0; i < ticksToProcess; i++)
        {
            ProcessTick();
        }

        return ticksToProcess > int.MaxValue ? int.MaxValue : (int)ticksToProcess;
    }

    private void ExecuteEntry(TimerEntry entry)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            entry.Callback();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Timer callback failed for timer '{TimerName}' ({TimerId})", entry.Name, entry.Id);
        }
        finally
        {
            _ = Stopwatch.GetTimestamp() - startedAt;
        }

        if (!entry.Repeat)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (!entry.Cancelled && _timersById.ContainsKey(entry.Id))
            {
                ScheduleEntry(entry, entry.Interval);
            }
        }
    }

    private void ProcessTick()
    {
        List<TimerEntry> dueEntries = [];

        lock (_syncRoot)
        {
            _currentTick++;
            var slotIndex = (int)(_currentTick % _wheel.Length);
            var bucket = _wheel[slotIndex];
            var node = bucket.First;

            while (node is not null)
            {
                var next = node.Next;
                var entry = node.Value;

                if (entry.Cancelled)
                {
                    bucket.Remove(node);
                    entry.Node = null;
                    node = next;

                    continue;
                }

                if (entry.RemainingRounds > 0)
                {
                    entry.RemainingRounds--;
                    node = next;

                    continue;
                }

                bucket.Remove(node);
                entry.Node = null;
                dueEntries.Add(entry);

                if (!entry.Repeat)
                {
                    RemoveFromIndexes(entry);
                }

                node = next;
            }
        }

        for (var i = 0; i < dueEntries.Count; i++)
        {
            ExecuteEntry(dueEntries[i]);
        }
    }

    private bool RemoveEntryById(string timerId)
    {
        if (!_timersById.TryGetValue(timerId, out var entry))
        {
            return false;
        }

        entry.Cancelled = true;

        if (entry.Node is not null)
        {
            _wheel[entry.SlotIndex].Remove(entry.Node);
            entry.Node = null;
        }

        RemoveFromIndexes(entry);

        return true;
    }

    private void RemoveFromIndexes(TimerEntry entry)
    {
        _timersById.Remove(entry.Id);

        if (!_timerIdsByName.TryGetValue(entry.Name, out var timerIds))
        {
            return;
        }

        timerIds.Remove(entry.Id);

        if (timerIds.Count == 0)
        {
            _timerIdsByName.Remove(entry.Name);
        }
    }

    private void ScheduleEntry(TimerEntry entry, TimeSpan dueTime)
    {
        var ticks = ToWheelTicks(dueTime);
        var targetTick = _currentTick + ticks;
        var slotIndex = (int)(targetTick % _wheel.Length);
        var rounds = (ticks - 1) / _wheel.Length;

        entry.Cancelled = false;
        entry.Node = _wheel[slotIndex].AddLast(entry);
        entry.RemainingRounds = rounds;
        entry.SlotIndex = slotIndex;
    }

    private long ToWheelTicks(TimeSpan dueTime)
    {
        var ticks = (long)Math.Ceiling(dueTime.TotalMilliseconds / _tickDurationMs);

        return Math.Max(1, ticks);
    }
}
