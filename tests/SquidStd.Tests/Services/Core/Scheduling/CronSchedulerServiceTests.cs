using Cronos;
using SquidStd.Services.Core.Services.Scheduling;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Services.Core.Scheduling;

public class CronSchedulerServiceTests
{
    [Fact]
    public void Fire_RunsHandler_AndReschedules()
    {
        var timer = new FakeTimerService();
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);
        var count = 0;
        scheduler.Schedule("tick", "* * * * *", _ => { count++; return Task.CompletedTask; });

        Assert.Equal(1, timer.Count); // one-shot registered

        timer.FireDue();              // fires -> job queued + rescheduled
        Assert.Equal(1, jobs.RunAll());
        Assert.Equal(1, count);
        Assert.Equal(1, timer.Count); // rescheduled

        timer.FireDue();
        jobs.RunAll();
        Assert.Equal(2, count);
    }

    [Fact]
    public void Jobs_ExposesRegisteredJob()
    {
        var timer = new FakeTimerService();
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);

        var id = scheduler.Schedule("snap", "* * * * *", _ => Task.CompletedTask);

        var info = Assert.Single(scheduler.Jobs);
        Assert.Equal(id, info.JobId);
        Assert.Equal("snap", info.Name);
        Assert.Equal("* * * * *", info.CronExpression);
        Assert.NotNull(info.NextOccurrenceUtc);
        Assert.False(info.IsRunning);
        Assert.Equal(0, info.RunCount);
    }

    [Fact]
    public void Schedule_InvalidCron_Throws()
    {
        var timer = new FakeTimerService();
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);

        Assert.Throws<CronFormatException>(() => { scheduler.Schedule("bad", "not a cron", _ => Task.CompletedTask); });
    }

    [Fact]
    public void Fire_WhileRunning_SkipsOverlappingOccurrence()
    {
        var timer = new FakeTimerService();
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);
        var count = 0;
        scheduler.Schedule("tick", "* * * * *", _ => { count++; return Task.CompletedTask; });

        timer.FireDue(); // running flag set, job queued (not yet run)
        timer.FireDue(); // still running -> occurrence skipped, no second job queued

        Assert.Equal(1, jobs.PendingCount);
        jobs.RunAll();
        Assert.Equal(1, count);
    }

    [Fact]
    public void Unschedule_StopsFutureFirings()
    {
        var timer = new FakeTimerService();
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);
        var count = 0;
        var id = scheduler.Schedule("tick", "* * * * *", _ => { count++; return Task.CompletedTask; });

        Assert.True(scheduler.Unschedule(id));
        Assert.Equal(0, timer.Count);
        Assert.Equal(0, timer.FireDue());
        jobs.RunAll();
        Assert.Equal(0, count);
        Assert.Empty(scheduler.Jobs);
    }

    [Fact]
    public void UnscheduleByName_RemovesAllMatching()
    {
        var timer = new FakeTimerService();
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);
        scheduler.Schedule("dup", "* * * * *", _ => Task.CompletedTask);
        scheduler.Schedule("dup", "* * * * *", _ => Task.CompletedTask);
        scheduler.Schedule("other", "* * * * *", _ => Task.CompletedTask);

        Assert.Equal(2, scheduler.UnscheduleByName("dup"));
        Assert.Single(scheduler.Jobs);
    }

    [Fact]
    public void Fire_HandlerThrows_IsLogged_AndKeepsRescheduling()
    {
        var timer = new FakeTimerService();
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);
        scheduler.Schedule("boom", "* * * * *", _ => throw new InvalidOperationException("boom"));

        timer.FireDue();
        jobs.RunAll(); // handler throws, swallowed
        Assert.Equal(1, timer.Count); // still rescheduled

        timer.FireDue();
        jobs.RunAll();
        Assert.Equal(1, timer.Count); // still alive
        Assert.Equal(0, Assert.Single(scheduler.Jobs).RunCount); // failures do not count as runs
    }

    [Fact]
    public void RealTimerWheel_FiresJob_WhenAdvancedPastAMinute()
    {
        var timer = new SquidStd.Services.Core.Services.TimerWheelService(
            new SquidStd.Core.Data.Timing.TimerWheelConfig
            {
                TickDuration = TimeSpan.FromMilliseconds(8),
                WheelSize = 512
            }
        );
        var jobs = new ManualJobSystem();
        using var scheduler = new CronSchedulerService(timer, jobs);
        var count = 0;
        scheduler.Schedule("tick", "* * * * *", _ => { count++; return Task.CompletedTask; });

        timer.UpdateTicksDelta(0);       // baseline
        timer.UpdateTicksDelta(61_000);  // advance just over one minute

        Assert.True(jobs.RunAll() >= 1);
        Assert.True(count >= 1);
    }
}
