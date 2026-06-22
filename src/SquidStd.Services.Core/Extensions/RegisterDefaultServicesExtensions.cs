using DryIoc;
using SquidStd.Abstractions.Data.Internal.Services;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Abstractions.Extensions.Container;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Core.Data.Jobs;
using SquidStd.Core.Data.Timing;
using SquidStd.Core.Interfaces.Config;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Jobs;
using SquidStd.Core.Interfaces.Threading;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Services.Core.Services;

namespace SquidStd.Services.Core.Extensions;

/// <summary>
/// Extension methods for registering the default SquidStd core services.
/// </summary>
public static class RegisterDefaultServicesExtensions
{
    /// <param name="container">Container that receives the core service registrations.</param>
    extension(IContainer container)
    {
        /// <summary>
        /// Registers the default SquidStd core services using the default config file location.
        /// </summary>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterCoreServices()
            => container.RegisterCoreServices("squidstd", Directory.GetCurrentDirectory());

        /// <summary>
        /// Registers the default SquidStd core services and config manager.
        /// </summary>
        /// <param name="configName">The logical config name or YAML file name.</param>
        /// <param name="configDirectory">The directory where the config file is searched.</param>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterCoreServices(string configName, string configDirectory)
        {
            container.RegisterDefaultCoreConfigSections();
            container.RegisterConfigManagerService(configName, configDirectory);
            container.RegisterEventBusService();
            container.RegisterJobSystemService();
            container.RegisterMainThreadDispatcherService();
            container.RegisterTimerWheelService();

            return container;
        }

        /// <summary>
        /// Registers the default config manager service as a singleton instance.
        /// </summary>
        /// <param name="configName">The logical config name or YAML file name.</param>
        /// <param name="configDirectory">The directory where the config file is searched.</param>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterConfigManagerService(string configName, string configDirectory)
        {
            var service = new ConfigManagerService(container, configName, configDirectory);
            container.RegisterInstance<IConfigManagerService>(service, IfAlreadyRegistered.Replace);
            container.RegisterInstance(service, IfAlreadyRegistered.Replace);
            container.AddToRegisterTypedList(
                new ServiceRegistrationData(typeof(IConfigManagerService), typeof(ConfigManagerService), -1000)
            );

            return container;
        }

        /// <summary>
        /// Registers the default SquidStd core configuration sections.
        /// </summary>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterDefaultCoreConfigSections()
        {
            container.RegisterConfigSection<JobsConfig>("jobs", static () => new JobsConfig(), -100);
            container.RegisterConfigSection<TimerWheelConfig>("timerWheel", static () => new TimerWheelConfig(), -90);

            return container;
        }

        /// <summary>
        /// Registers the default event bus service in the container.
        /// </summary>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterEventBusService()
        {
            return container.RegisterStdService<IEventBus, EventBusService>(-1);
        }

        /// <summary>
        /// Registers the default job system service in the container.
        /// </summary>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterJobSystemService()
        {
            container.RegisterConfigSection<JobsConfig>("jobs", static () => new JobsConfig(), -100);

            return container.RegisterStdService<IJobSystem, JobSystemService>(-1);
        }

        /// <summary>
        /// Registers the default main-thread dispatcher service in the container.
        /// </summary>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterMainThreadDispatcherService()
        {
            return container.RegisterStdService<IMainThreadDispatcher, MainThreadDispatcherService>(-1);
        }

        /// <summary>
        /// Registers the default timer wheel service in the container.
        /// </summary>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterTimerWheelService()
        {
            container.RegisterConfigSection<TimerWheelConfig>("timerWheel", static () => new TimerWheelConfig(), -90);

            return container.RegisterStdService<ITimerService, TimerWheelService>(-1);
        }
    }
}
