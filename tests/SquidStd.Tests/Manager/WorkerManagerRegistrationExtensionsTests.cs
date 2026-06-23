using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Services.Core.Services.Scheduling;
using SquidStd.Tests.Manager.Support;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Extensions;
using SquidStd.Workers.Manager.Interfaces;
using SquidStd.Workers.Manager.Services;

namespace SquidStd.Tests.Manager;

public class WorkerManagerRegistrationExtensionsTests
{
    private static Container NewContainer()
    {
        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging();
        // RegisterCoreServices normally provides ITimerService; supply a fake so the sweep service resolves.
        container.RegisterInstance<ITimerService>(new FakeTimerService());
        // ConfigManager normally registers config instances; register one directly for the test.
        container.RegisterInstance(new WorkerManagerConfig());

        return container;
    }

    [Fact]
    public void AddWorkerManager_RegistersResolvableServices()
    {
        using var container = NewContainer();

        container.AddWorkerManager();

        Assert.NotNull(container.Resolve<IWorkerRegistry>());
        Assert.NotNull(container.Resolve<IJobScheduler>());
        Assert.NotNull(container.Resolve<HeartbeatCollectorService>());
        Assert.NotNull(container.Resolve<WorkerOfflineSweepService>());
    }

    [Fact]
    public void AddWorkerManager_RegistersTimerWheelPump_WhenAbsent()
    {
        using var container = NewContainer();

        container.AddWorkerManager();

        Assert.True(container.IsRegistered<TimerWheelPumpService>());
    }

    [Fact]
    public void AddWorkerManager_SharesRegistryInstanceAcrossInterfaceAndConcrete()
    {
        using var container = NewContainer();

        container.AddWorkerManager();

        Assert.Same(container.Resolve<IWorkerRegistry>(), container.Resolve<WorkerRegistry>());
    }
}
