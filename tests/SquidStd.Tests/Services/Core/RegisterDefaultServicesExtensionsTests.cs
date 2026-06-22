using DryIoc;
using SquidStd.Abstractions.Data.Internal.Config;
using SquidStd.Abstractions.Data.Internal.Services;
using SquidStd.Core.Data.Jobs;
using SquidStd.Core.Data.Timing;
using SquidStd.Core.Interfaces.Config;
using SquidStd.Services.Core.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Services.Core;

public class RegisterDefaultServicesExtensionsTests
{
    [Fact]
    public void RegisterConfigManagerService_RegistersSingletonInstance()
    {
        using var temp = new TempDirectory();
        using var container = new DryIoc.Container();

        container.RegisterConfigManagerService("app", temp.Path);

        var first = container.Resolve<IConfigManagerService>();
        var second = container.Resolve<IConfigManagerService>();

        Assert.Same(first, second);
        Assert.Equal(Path.Combine(temp.Path, "app.yaml"), first.ConfigPath);
    }

    [Fact]
    public void RegisterConfigManagerService_AddsServiceRegistrationDataWithEarliestPriority()
    {
        using var temp = new TempDirectory();
        using var container = new DryIoc.Container();

        container.RegisterConfigManagerService("app", temp.Path);

        var entry = Assert.Single(
            container.Resolve<List<ServiceRegistrationData>>(),
            registration => registration.ServiceType == typeof(IConfigManagerService)
        );

        Assert.Equal(typeof(ConfigManagerService), entry.ImplementationType);
        Assert.Equal(-1000, entry.Priority);
    }

    [Fact]
    public void RegisterDefaultCoreConfigSections_RegistersJobsAndTimerWheelMetadata()
    {
        using var container = new DryIoc.Container();

        container.RegisterDefaultCoreConfigSections();

        var entries = container.Resolve<List<ConfigRegistrationData>>();

        Assert.Contains(entries, entry => entry.SectionName == "jobs" && entry.ConfigType == typeof(JobsConfig));
        Assert.Contains(entries, entry => entry.SectionName == "timerWheel" && entry.ConfigType == typeof(TimerWheelConfig));
        Assert.False(container.IsRegistered<JobsConfig>());
        Assert.False(container.IsRegistered<TimerWheelConfig>());
    }

    [Fact]
    public async Task RegisterCoreServices_StartsConfigManagerBeforeResolvingConfigConsumers()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            temp.Combine("app.yaml"),
            """
            jobs:
              WorkerThreadCount: 2
              ShutdownTimeoutSeconds: 3
            timerWheel:
              TickDuration: 00:00:00.0100000
              WheelSize: 32
            """
        );
        using var container = new DryIoc.Container();
        container.RegisterCoreServices("app", temp.Path);

        var manager = container.Resolve<IConfigManagerService>();
        await ((ConfigManagerService)manager).StartAsync(CancellationToken.None);

        Assert.Equal(2, container.Resolve<JobsConfig>().WorkerThreadCount);
        Assert.Equal(3, container.Resolve<JobsConfig>().ShutdownTimeoutSeconds);
        Assert.Equal(TimeSpan.FromMilliseconds(10), container.Resolve<TimerWheelConfig>().TickDuration);
        Assert.Equal(32, container.Resolve<TimerWheelConfig>().WheelSize);
    }
}
