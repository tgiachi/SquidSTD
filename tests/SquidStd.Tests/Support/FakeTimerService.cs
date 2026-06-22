using SquidStd.Core.Interfaces.Timing;

namespace SquidStd.Tests.Support;

/// <summary>
/// In-memory <see cref="ITimerService" /> for tests. Timers do not fire on their own:
/// call <see cref="FireDue" /> to invoke and clear the currently-registered timers
/// (one-shot semantics). <see cref="UpdateTicksDelta" /> only records that a pump ran.
/// </summary>
public sealed class FakeTimerService : ITimerService
{
    private readonly Dictionary<string, (string Name, Action Callback)> _timers = new(StringComparer.Ordinal);

    public int Count => _timers.Count;

    public int TickUpdates { get; private set; }

    public ManualResetEventSlim Pumped { get; } = new(false);

    public string RegisterTimer(string name, TimeSpan interval, Action callback, TimeSpan? delay = null, bool repeat = false)
    {
        var id = Guid.NewGuid().ToString("N");
        _timers[id] = (name, callback);

        return id;
    }

    public void UnregisterAllTimers()
        => _timers.Clear();

    public bool UnregisterTimer(string timerId)
        => _timers.Remove(timerId);

    public int UnregisterTimersByName(string name)
    {
        var ids = _timers.Where(kv => kv.Value.Name == name).Select(kv => kv.Key).ToArray();

        foreach (var id in ids)
        {
            _timers.Remove(id);
        }

        return ids.Length;
    }

    public int UpdateTicksDelta(long timestampMilliseconds)
    {
        TickUpdates++;
        Pumped.Set();

        return 0;
    }

    /// <summary>Invokes and removes every currently-registered timer; returns how many fired.</summary>
    public int FireDue()
    {
        var snapshot = _timers.ToArray();

        foreach (var kv in snapshot)
        {
            _timers.Remove(kv.Key);
        }

        foreach (var kv in snapshot)
        {
            kv.Value.Callback();
        }

        return snapshot.Length;
    }
}
