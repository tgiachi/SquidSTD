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
}
