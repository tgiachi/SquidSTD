using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Extensions;
using SquidStd.Workers.Interfaces;
using SquidStd.Workers.Services;

namespace SquidStd.Tests.Workers;

public class WorkersRegistrationExtensionsTests
{
    private static Container NewContainer()
    {
        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging();
        // ConfigManager normally registers config instances; register one directly for the test.
        container.RegisterInstance(new WorkersConfig { WorkerId = "w1", MaxConcurrency = 4 });

        return container;
    }

    [Fact]
    public void AddWorkers_RegistersResolvableServices()
    {
        using var container = NewContainer();

        container.AddWorkers();

        Assert.NotNull(container.Resolve<IWorkerState>());
        Assert.NotNull(container.Resolve<IJobDispatcher>());
        Assert.NotNull(container.Resolve<WorkerConsumerService>());
        Assert.NotNull(container.Resolve<WorkerHeartbeatService>());
    }

    [Fact]
    public async Task AddJobHandler_MakesHandlerReachableFromDispatcher()
    {
        using var container = NewContainer();

        container.AddWorkers();
        container.AddJobHandler<EchoJobHandler>();

        var dispatcher = container.Resolve<IJobDispatcher>();
        await dispatcher.DispatchAsync(new("echo", new Dictionary<string, string>()), CancellationToken.None);

        Assert.Equal(1, EchoJobHandler.Calls);
    }

    private sealed class EchoJobHandler : IJobHandler
    {
        public static int Calls;

        public string JobName => "echo";

        public Task HandleAsync(SquidStd.Workers.Abstractions.Data.JobRequest job, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);

            return Task.CompletedTask;
        }
    }
}
