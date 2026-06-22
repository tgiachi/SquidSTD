using SquidStd.Core.Interfaces.Timing;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class TimerWheelServiceTests
{
    [Fact]
    public void CallbackException_DoesNotStopOtherTimers()
    {
        ITimerService timer = NewService(8, 8);
        var calls = 0;
        timer.RegisterTimer("bad", TimeSpan.FromMilliseconds(8), () => throw new InvalidOperationException("boom"));
        timer.RegisterTimer("good", TimeSpan.FromMilliseconds(8), () => calls++);

        timer.UpdateTicksDelta(0);
        timer.UpdateTicksDelta(8);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Ctor_InvalidConfig_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimerWheelService(new() { TickDuration = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TimerWheelService(new() { TickDuration = TimeSpan.FromMilliseconds(8), WheelSize = 0 })
        );
    }

    [Fact]
    public void Delay_PostponesFirstExecutionThenUsesInterval()
    {
        ITimerService timer = NewService();
        var timestamps = new List<long>();
        var current = 0L;
        timer.RegisterTimer(
            "delayed",
            TimeSpan.FromMilliseconds(8),
            () => timestamps.Add(current),
            TimeSpan.FromMilliseconds(24),
            true
        );

        timer.UpdateTicksDelta(0);

        for (var timestamp = 8; timestamp <= 48; timestamp += 8)
        {
            current = timestamp;
            timer.UpdateTicksDelta(timestamp);
        }

        Assert.Equal([24L, 32L, 40L, 48L], timestamps);
    }

    [Fact]
    public void OneShot_FiresExactlyOnceAtDueTime()
    {
        ITimerService timer = NewService(8, 8);
        var calls = 0;
        timer.RegisterTimer("once", TimeSpan.FromMilliseconds(8), () => calls++);

        timer.UpdateTicksDelta(0);
        timer.UpdateTicksDelta(8);
        timer.UpdateTicksDelta(16);
        timer.UpdateTicksDelta(24);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void RegisterTimer_ReturnsNonEmptyDistinctIds()
    {
        ITimerService timer = NewService();

        var first = timer.RegisterTimer("timer", TimeSpan.FromMilliseconds(8), () => { });
        var second = timer.RegisterTimer("timer", TimeSpan.FromMilliseconds(8), () => { });

        Assert.False(string.IsNullOrEmpty(first));
        Assert.False(string.IsNullOrEmpty(second));
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Repeating_FiresEveryInterval()
    {
        ITimerService timer = NewService(8, 8);
        var calls = 0;
        timer.RegisterTimer("repeat", TimeSpan.FromMilliseconds(16), () => calls++, repeat: true);

        timer.UpdateTicksDelta(0);
        timer.UpdateTicksDelta(80);

        Assert.Equal(5, calls);
    }

    [Fact]
    public async Task StopAsync_ClearsState()
    {
        var timer = NewService();
        ITimerService service = timer;
        service.RegisterTimer("first", TimeSpan.FromMilliseconds(8), () => { });
        service.RegisterTimer("second", TimeSpan.FromMilliseconds(8), () => { });

        await timer.StopAsync(CancellationToken.None);

        Assert.Equal(0, service.UnregisterTimersByName("first"));
        Assert.Equal(0, service.UnregisterTimersByName("second"));
    }

    [Fact]
    public void UnregisterTimer_BeforeDueTime_PreventsCallback()
    {
        ITimerService timer = NewService(8, 8);
        var calls = 0;
        var timerId = timer.RegisterTimer("cancel", TimeSpan.FromMilliseconds(8), () => calls++);

        timer.UpdateTicksDelta(0);
        Assert.True(timer.UnregisterTimer(timerId));
        timer.UpdateTicksDelta(16);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void UnregisterTimersByName_RemovesEveryMatchingTimer()
    {
        ITimerService timer = NewService();
        timer.RegisterTimer("group", TimeSpan.FromMilliseconds(8), () => { });
        timer.RegisterTimer("group", TimeSpan.FromMilliseconds(8), () => { });
        timer.RegisterTimer("other", TimeSpan.FromMilliseconds(8), () => { });

        var removed = timer.UnregisterTimersByName("group");

        Assert.Equal(2, removed);
        Assert.Equal(0, timer.UnregisterTimersByName("group"));
        Assert.Equal(1, timer.UnregisterTimersByName("other"));
    }

    [Fact]
    public void UpdateTicksDelta_AdvancesByWholeTicks()
    {
        ITimerService timer = NewService();
        timer.UpdateTicksDelta(0);

        var processed = timer.UpdateTicksDelta(24);

        Assert.Equal(3, processed);
    }

    private static TimerWheelService NewService(int tickDurationMs = 8, int wheelSize = 16)
        => new(
            new()
            {
                TickDuration = TimeSpan.FromMilliseconds(tickDurationMs),
                WheelSize = wheelSize
            }
        );
}
