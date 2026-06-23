using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Workers.Abstractions;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Abstractions.Types;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Services;

namespace SquidStd.Tests.Workers;

public class WorkerHeartbeatServiceTests
{
    private static (IMessageTopic topic, WorkerState state) NewMessaging(WorkersConfig config)
    {
        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging();

        return (container.Resolve<IMessageTopic>(), new WorkerState(config));
    }

    [Fact]
    public async Task StartAsync_PublishesHeartbeatImmediately()
    {
        var config = new WorkersConfig { WorkerId = "w1", MaxConcurrency = 8, HeartbeatIntervalSeconds = 60 };
        var (topic, state) = NewMessaging(config);

        var received = new TaskCompletionSource<WorkerHeartbeat>();
        using var _ = topic.Subscribe<WorkerHeartbeat>(
            WorkerChannels.HeartbeatTopic,
            (hb, _) => { received.TrySetResult(hb); return Task.CompletedTask; });

        var service = new WorkerHeartbeatService(topic, state, config);
        await service.StartAsync();

        var heartbeat = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync();

        Assert.Equal("w1", heartbeat.WorkerId);
        Assert.Equal(8, heartbeat.MaxConcurrency);
        Assert.Equal(0, heartbeat.ActiveJobs);
        Assert.Equal(WorkerStatusType.Idle, heartbeat.Status);
    }

    [Fact]
    public async Task Heartbeat_ReflectsBusyStateWithActiveJobs()
    {
        var config = new WorkersConfig { WorkerId = "w1", MaxConcurrency = 8, HeartbeatIntervalSeconds = 60 };
        var (topic, state) = NewMessaging(config);
        state.JobStarted();

        var received = new TaskCompletionSource<WorkerHeartbeat>();
        using var _ = topic.Subscribe<WorkerHeartbeat>(
            WorkerChannels.HeartbeatTopic,
            (hb, _) => { received.TrySetResult(hb); return Task.CompletedTask; });

        var service = new WorkerHeartbeatService(topic, state, config);
        await service.StartAsync();

        var heartbeat = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync();

        Assert.Equal(1, heartbeat.ActiveJobs);
        Assert.Equal(WorkerStatusType.Busy, heartbeat.Status);
    }
}
