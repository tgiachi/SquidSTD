using DryIoc;
using Microsoft.AspNetCore.Http.HttpResults;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Abstractions.Types;
using SquidStd.Workers.Manager.Data;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Endpoints;
using SquidStd.Workers.Manager.Services;

namespace SquidStd.Tests.Manager;

public class WorkerManagerEndpointsTests
{
    private static WorkerRegistry RegistryWith(params string[] workerIds)
    {
        var registry = new WorkerRegistry(new WorkerManagerConfig());
        foreach (var id in workerIds)
        {
            registry.Record(new WorkerHeartbeat(id, DateTime.UtcNow, WorkerStatusType.Idle, 0, 8));
        }

        return registry;
    }

    private static JobScheduler NewScheduler()
    {
        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging();

        return new JobScheduler(container.Resolve<IMessageQueue>(), new WorkerManagerConfig());
    }

    [Fact]
    public void GetWorkers_ReturnsOkWithAll()
    {
        var result = WorkerManagerEndpoints.GetWorkers(RegistryWith("w1", "w2"));

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public void GetWorker_ReturnsOk_WhenPresent()
    {
        var result = WorkerManagerEndpoints.GetWorker("w1", RegistryWith("w1"));

        Assert.IsType<Ok<WorkerInfo>>(result.Result);
    }

    [Fact]
    public void GetWorker_ReturnsNotFound_WhenAbsent()
    {
        var result = WorkerManagerEndpoints.GetWorker("missing", RegistryWith("w1"));

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task EnqueueJob_ReturnsAccepted_AndSchedulesJob()
    {
        var result = await WorkerManagerEndpoints.EnqueueJob(
            new EnqueueJobRequest("resize", new Dictionary<string, string>()),
            NewScheduler(),
            CancellationToken.None);

        Assert.IsType<Accepted>(result.Result);
    }

    [Fact]
    public async Task EnqueueJob_ReturnsBadRequest_WhenJobNameBlank()
    {
        var result = await WorkerManagerEndpoints.EnqueueJob(
            new EnqueueJobRequest("   ", null),
            NewScheduler(),
            CancellationToken.None);

        Assert.IsType<BadRequest<string>>(result.Result);
    }
}
