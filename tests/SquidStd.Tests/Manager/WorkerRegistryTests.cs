using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Abstractions.Types;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Services;

namespace SquidStd.Tests.Manager;

public class WorkerRegistryTests
{
    private static WorkerRegistry NewRegistry(int offlineTimeoutSeconds = 30)
        => new(new WorkerManagerConfig { OfflineTimeoutSeconds = offlineTimeoutSeconds });

    private static WorkerHeartbeat Heartbeat(string id, WorkerStatusType status, int activeJobs = 0)
        => new(id, DateTime.UtcNow, status, activeJobs, 8);

    [Fact]
    public void Record_NewWorker_ReturnsDiscoveredTransitionAndStores()
    {
        var registry = NewRegistry();

        var change = registry.Record(Heartbeat("w1", WorkerStatusType.Idle));

        Assert.NotNull(change);
        Assert.Equal("w1", change!.WorkerId);
        Assert.Null(change.OldStatus);
        Assert.Equal(WorkerStatusType.Idle, change.NewStatus);

        var info = registry.Get("w1");
        Assert.NotNull(info);
        Assert.Equal(WorkerStatusType.Idle, info!.Status);
        Assert.Equal(8, info.MaxConcurrency);
    }

    [Fact]
    public void Record_ExistingWorker_IdleToBusy_NoTransitionButStateUpdated()
    {
        var registry = NewRegistry();
        registry.Record(Heartbeat("w1", WorkerStatusType.Idle));

        var change = registry.Record(Heartbeat("w1", WorkerStatusType.Busy, activeJobs: 2));

        Assert.Null(change);
        Assert.Equal(WorkerStatusType.Busy, registry.Get("w1")!.Status);
        Assert.Equal(2, registry.Get("w1")!.ActiveJobs);
    }

    [Fact]
    public void Sweep_MarksOverdueWorkerOffline_AndReturnsTransition()
    {
        var registry = NewRegistry(offlineTimeoutSeconds: 30);
        registry.Record(Heartbeat("w1", WorkerStatusType.Idle));

        var changes = registry.Sweep(DateTime.UtcNow.AddSeconds(31));

        var change = Assert.Single(changes);
        Assert.Equal("w1", change.WorkerId);
        Assert.Equal(WorkerStatusType.Offline, change.NewStatus);
        Assert.Equal(WorkerStatusType.Offline, registry.Get("w1")!.Status);
    }

    [Fact]
    public void Sweep_DoesNotReEmitAlreadyOfflineWorker()
    {
        var registry = NewRegistry(offlineTimeoutSeconds: 30);
        registry.Record(Heartbeat("w1", WorkerStatusType.Idle));
        registry.Sweep(DateTime.UtcNow.AddSeconds(31));

        var changes = registry.Sweep(DateTime.UtcNow.AddSeconds(62));

        Assert.Empty(changes);
    }

    [Fact]
    public void Record_AfterOffline_ReturnsReturnTransition()
    {
        var registry = NewRegistry(offlineTimeoutSeconds: 30);
        registry.Record(Heartbeat("w1", WorkerStatusType.Idle));
        registry.Sweep(DateTime.UtcNow.AddSeconds(31));

        var change = registry.Record(Heartbeat("w1", WorkerStatusType.Busy));

        Assert.NotNull(change);
        Assert.Equal(WorkerStatusType.Offline, change!.OldStatus);
        Assert.Equal(WorkerStatusType.Busy, change.NewStatus);
    }

    [Fact]
    public void GetAll_ReturnsAllKnownWorkers()
    {
        var registry = NewRegistry();
        registry.Record(Heartbeat("w1", WorkerStatusType.Idle));
        registry.Record(Heartbeat("w2", WorkerStatusType.Busy));

        Assert.Equal(2, registry.GetAll().Count);
        Assert.Null(registry.Get("missing"));
    }
}
