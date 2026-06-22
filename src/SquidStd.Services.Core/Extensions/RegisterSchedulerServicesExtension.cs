using DryIoc;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Core.Data.Timing;
using SquidStd.Core.Interfaces.Scheduling;
using SquidStd.Services.Core.Services.Scheduling;

namespace SquidStd.Services.Core.Extensions;

/// <summary>
/// Extension methods for registering the SquidStd cron scheduler.
/// </summary>
public static class RegisterSchedulerServicesExtension
{
    /// <param name="container">Container that receives the scheduler registrations.</param>
    extension(IContainer container)
    {
        /// <summary>
        /// Registers the timer wheel pump and the cron scheduler. Must be called after
        /// <c>RegisterCoreServices</c> so that <c>ITimerService</c> and <c>IJobSystem</c> exist.
        /// </summary>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterSchedulerServices()
        {
            container.RegisterConfigSection("timerWheelPump", static () => new TimerWheelPumpConfig(), -90);
            container.RegisterStdService<TimerWheelPumpService, TimerWheelPumpService>(-1);
            container.RegisterStdService<ICronScheduler, CronSchedulerService>(-1);

            return container;
        }
    }
}
