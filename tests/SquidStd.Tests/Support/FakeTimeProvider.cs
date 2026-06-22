namespace SquidStd.Tests.Support;

/// <summary>
/// Test TimeProvider with a manually advanced clock and an inert timer (so periodic sweeps
/// never fire on their own; tests invoke the sweep explicitly).
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset start)
    {
        _utcNow = start;
    }

    public override DateTimeOffset GetUtcNow()
        => _utcNow;

    public void Advance(TimeSpan delta)
        => _utcNow = _utcNow.Add(delta);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        => new InertTimer();

    private sealed class InertTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period)
            => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
