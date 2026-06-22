using DryIoc;
using SquidStd.Core.Data.Jobs;
using SquidStd.Core.Data.Timing;
using SquidStd.Core.Interfaces.Jobs;
using SquidStd.Core.Interfaces.Scheduling;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Services.Core.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Services.Core.Services.Scheduling;

namespace SquidStd.Tests.Services.Core.Scheduling;

public class CronSchedulerRegistrationTests
{
    [Fact]
    public void RegisterSchedulerServices_ResolvesSchedulerAndPump()
    {
        var container = new Container();
        container.RegisterInstance(new TimerWheelConfig());
        container.RegisterInstance(new TimerWheelPumpConfig());
        container.RegisterInstance(new JobsConfig { WorkerThreadCount = 1 });
        container.Register<ITimerService, TimerWheelService>(Reuse.Singleton);
        container.Register<IJobSystem, JobSystemService>(Reuse.Singleton);

        container.RegisterSchedulerServices();

        Assert.NotNull(container.Resolve<ICronScheduler>());
        Assert.NotNull(container.Resolve<TimerWheelPumpService>());
    }
}
