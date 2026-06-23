using SquidStd.Core.Interfaces.Timing;

namespace SquidStd.Tests.Manager.Support;

/// <summary>Captures timer registration for tests without running a real timer wheel.</summary>
public sealed class FakeTimerService : ITimerService
{
    public string? RegisteredName { get; private set; }

    public TimeSpan RegisteredInterval { get; private set; }

    public bool RegisteredRepeat { get; private set; }

    public string? UnregisteredId { get; private set; }

    public string RegisterTimer(string name, TimeSpan interval, Action callback, TimeSpan? delay = null, bool repeat = false)
    {
        RegisteredName = name;
        RegisteredInterval = interval;
        RegisteredRepeat = repeat;

        return "timer-1";
    }

    public void UnregisterAllTimers()
    {
    }

    public bool UnregisterTimer(string timerId)
    {
        UnregisteredId = timerId;

        return true;
    }

    public int UnregisterTimersByName(string name)
        => 0;

    public int UpdateTicksDelta(long timestampMilliseconds)
        => 0;
}
