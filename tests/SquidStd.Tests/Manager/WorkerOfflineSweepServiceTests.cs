using SquidStd.Services.Core.Services;
using SquidStd.Tests.Manager.Support;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Abstractions.Types;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Data.Events;
using SquidStd.Workers.Manager.Services;
using SquidStd.Core.Interfaces.Events;

namespace SquidStd.Tests.Manager;

public class WorkerOfflineSweepServiceTests
{
    [Fact]
    public async Task StartAsync_RegistersRepeatingTimer_StopAsyncUnregisters()
    {
        var timer = new FakeTimerService();
        var registry = new WorkerRegistry(new WorkerManagerConfig());
        var service = new WorkerOfflineSweepService(timer, registry, new EventBusService(), new WorkerManagerConfig { SweepIntervalSeconds = 5 });

        await service.StartAsync();

        Assert.Equal("worker-offline-sweep", timer.RegisteredName);
        Assert.Equal(TimeSpan.FromSeconds(5), timer.RegisteredInterval);
        Assert.True(timer.RegisteredRepeat);

        await service.StopAsync();
        Assert.Equal("timer-1", timer.UnregisteredId);
    }

    [Fact]
    public async Task RunSweepAsync_MarksOverdueWorkerOffline_AndPublishesTransition()
    {
        var registry = new WorkerRegistry(new WorkerManagerConfig { OfflineTimeoutSeconds = 1 });
        registry.Record(new WorkerHeartbeat("w1", DateTime.UtcNow, WorkerStatusType.Idle, 0, 8));
        await Task.Delay(1100);

        var eventBus = new EventBusService();
        var offline = new TaskCompletionSource<WorkerStatusChangedEvent>();
        eventBus.RegisterAsyncListener(new DelegateListener(e => offline.TrySetResult(e)));

        var service = new WorkerOfflineSweepService(new FakeTimerService(), registry, eventBus, new WorkerManagerConfig());
        await service.RunSweepAsync();

        var change = await offline.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("w1", change.WorkerId);
        Assert.Equal(WorkerStatusType.Offline, change.NewStatus);
        Assert.Equal(WorkerStatusType.Offline, registry.Get("w1")!.Status);
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
