using DryIoc;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Interfaces;
using SquidStd.Workers.Services;

namespace SquidStd.Workers.Extensions;

/// <summary>
/// DryIoc registration helpers for the worker runtime.
/// </summary>
public static class WorkersRegistrationExtensions
{
    /// <summary>
    /// Registers the worker runtime: the "workers" config section, shared state, job dispatcher, and the
    /// consumer + heartbeat lifecycle services.
    /// </summary>
    public static IContainer AddWorkers(this IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.RegisterConfigSection("workers", static () => new WorkersConfig(), -50);

        container.Register<IWorkerState, WorkerState>(Reuse.Singleton);
        container.Register<IJobDispatcher, JobDispatcher>(Reuse.Singleton);

        container.RegisterStdService<WorkerConsumerService, WorkerConsumerService>(100);
        container.RegisterStdService<WorkerHeartbeatService, WorkerHeartbeatService>(110);

        return container;
    }

    /// <summary>
    /// Registers a job handler so the dispatcher can route jobs to it by name.
    /// </summary>
    public static IContainer AddJobHandler<THandler>(this IContainer container)
        where THandler : class, IJobHandler
    {
        ArgumentNullException.ThrowIfNull(container);

        container.Register<IJobHandler, THandler>(Reuse.Singleton);

        return container;
    }
}
