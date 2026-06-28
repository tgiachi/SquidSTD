namespace SquidStd.Tests.Support;

/// <summary>
///     Test TimeProvider with a manually advanced clock and an inert timer (so periodic sweeps
///     never fire on their own; tests invoke the sweep explicitly).
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset start)
    {
        _utcNow = start;
    }

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        return new InertTimer();
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    private sealed class InertTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return true;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
