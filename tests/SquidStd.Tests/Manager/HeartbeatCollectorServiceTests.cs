using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Workers.Abstractions;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Abstractions.Types;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Data.Events;
using SquidStd.Workers.Manager.Services;

namespace SquidStd.Tests.Manager;

public class HeartbeatCollectorServiceTests
{
    [Fact]
    public async Task Collector_RecordsHeartbeat_AndPublishesDiscoveredEvent()
    {
        var container = new Container();
        var eventBus = new EventBusService();
        container.RegisterInstance<IEventBus>(eventBus);
        container.AddInMemoryMessaging();

        var topic = container.Resolve<IMessageTopic>();
        var registry = new WorkerRegistry(new WorkerManagerConfig());

        var discovered = new TaskCompletionSource<WorkerStatusChangedEvent>();
        eventBus.RegisterAsyncListener(new DelegateListener(e => discovered.TrySetResult(e)));

        var service = new HeartbeatCollectorService(topic, registry, eventBus, new WorkerManagerConfig());
        await service.StartAsync();

        await topic.PublishAsync(
            WorkerChannels.HeartbeatTopic,
            new WorkerHeartbeat("w1", DateTime.UtcNow, WorkerStatusType.Idle, 0, 8));

        var change = await discovered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync();

        Assert.Equal("w1", change.WorkerId);
        Assert.Null(change.OldStatus);
        Assert.NotNull(registry.Get("w1"));
    }

    private sealed class DelegateListener : IAsyncEventListener<WorkerStatusChangedEvent>
    {
        private readonly Action<WorkerStatusChangedEvent> _onEvent;

        public DelegateListener(Action<WorkerStatusChangedEvent> onEvent)
        {
            _onEvent = onEvent;
        }

        public Task HandleAsync(WorkerStatusChangedEvent eventData, CancellationToken cancellationToken)
        {
            _onEvent(eventData);

            return Task.CompletedTask;
        }
    }
}
