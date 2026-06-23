using DryIoc;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Core.Data.Timing;
using SquidStd.Services.Core.Services.Scheduling;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Interfaces;
using SquidStd.Workers.Manager.Services;

namespace SquidStd.Workers.Manager.Extensions;

/// <summary>
/// DryIoc registration helpers for the worker manager.
/// </summary>
public static class WorkerManagerRegistrationExtensions
{
    /// <summary>
    /// Registers the worker manager: config section, registry, job scheduler, the collector and sweep
    /// lifecycle services, and the timer-wheel pump (only if it is not already registered).
    /// </summary>
    public static IContainer AddWorkerManager(this IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.RegisterConfigSection("workerManager", static () => new WorkerManagerConfig(), -50);

        container.Register<WorkerRegistry>(Reuse.Singleton);
        container.RegisterMapping<IWorkerRegistry, WorkerRegistry>();
        container.Register<IJobScheduler, JobScheduler>(Reuse.Singleton);

        container.RegisterStdService<HeartbeatCollectorService, HeartbeatCollectorService>(100);
        container.RegisterStdService<WorkerOfflineSweepService, WorkerOfflineSweepService>(110);

        if (!container.IsRegistered<TimerWheelPumpService>())
        {
            container.RegisterConfigSection("timerWheelPump", static () => new TimerWheelPumpConfig(), -90);
            container.RegisterStdService<TimerWheelPumpService, TimerWheelPumpService>(-1);
        }

        return container;
    }
}
