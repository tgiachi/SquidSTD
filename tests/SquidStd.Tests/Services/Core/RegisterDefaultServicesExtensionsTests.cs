using DryIoc;
using SquidStd.Abstractions.Data.Internal.Config;
using SquidStd.Abstractions.Data.Internal.Services;
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Core.Data.Jobs;
using SquidStd.Core.Data.Metrics;
using SquidStd.Core.Data.Storage;
using SquidStd.Storage.Abstractions.Data.Config;
using SquidStd.Core.Data.Timing;
using SquidStd.Core.Interfaces.Config;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Secrets;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Services.Core.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Services.Core.Services.Storage;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Services.Core;

public class RegisterDefaultServicesExtensionsTests
{
    [Fact]
    public void RegisterConfigManagerService_AddsServiceRegistrationDataWithEarliestPriority()
    {
        using var temp = new TempDirectory();
        using var container = new Container();

        container.RegisterConfigManagerService("app", temp.Path);

        var entry = Assert.Single(
            container.Resolve<List<ServiceRegistrationData>>(),
            registration => registration.ServiceType == typeof(IConfigManagerService)
        );

        Assert.Equal(typeof(ConfigManagerService), entry.ImplementationType);
        Assert.Equal(-1000, entry.Priority);
    }

    [Fact]
    public void RegisterConfigManagerService_RegistersSingletonInstance()
    {
        using var temp = new TempDirectory();
        using var container = new Container();

        container.RegisterConfigManagerService("app", temp.Path);

        var first = container.Resolve<IConfigManagerService>();
        var second = container.Resolve<IConfigManagerService>();

        Assert.Same(first, second);
        Assert.Equal(Path.Combine(temp.Path, "app.yaml"), first.ConfigPath);
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
        using var container = new Container();
        container.RegisterCoreServices("app", temp.Path);

        var manager = container.Resolve<IConfigManagerService>();
        await ((ConfigManagerService)manager).StartAsync(CancellationToken.None);

        Assert.Equal(2, container.Resolve<JobsConfig>().WorkerThreadCount);
        Assert.Equal(3, container.Resolve<JobsConfig>().ShutdownTimeoutSeconds);
        Assert.Equal(TimeSpan.FromMilliseconds(10), container.Resolve<TimerWheelConfig>().TickDuration);
        Assert.Equal(32, container.Resolve<TimerWheelConfig>().WheelSize);
    }

    [Fact]
    public void RegisterDefaultCoreConfigSections_RegistersJobsAndTimerWheelMetadata()
    {
        using var container = new Container();

        container.RegisterDefaultCoreConfigSections();

        var entries = container.Resolve<List<ConfigRegistrationData>>();

        Assert.Contains(entries, entry => entry.SectionName == "jobs" && entry.ConfigType == typeof(JobsConfig));
        Assert.Contains(entries, entry => entry.SectionName == "timerWheel" && entry.ConfigType == typeof(TimerWheelConfig));
        Assert.False(container.IsRegistered<JobsConfig>());
        Assert.False(container.IsRegistered<TimerWheelConfig>());
    }

    [Fact]
    public void RegisterDefaultCoreConfigSections_RegistersLoggerMetadata()
    {
        using var container = new Container();

        container.RegisterDefaultCoreConfigSections();

        var entries = container.Resolve<List<ConfigRegistrationData>>();

        Assert.Contains(
            entries,
            entry => entry.SectionName == "logger" && entry.ConfigType == typeof(SquidStdLoggerOptions)
        );
        Assert.False(container.IsRegistered<SquidStdLoggerOptions>());
    }

    [Fact]
    public void RegisterDefaultCoreConfigSections_RegistersMetricsMetadata()
    {
        using var container = new Container();

        container.RegisterDefaultCoreConfigSections();

        var entries = container.Resolve<List<ConfigRegistrationData>>();

        Assert.Contains(entries, entry => entry.SectionName == "metrics" && entry.ConfigType == typeof(MetricsConfig));
        Assert.False(container.IsRegistered<MetricsConfig>());
    }

    [Fact]
    public void RegisterDefaultCoreConfigSections_RegistersStorageAndSecretsMetadata()
    {
        using var container = new Container();

        container.RegisterDefaultCoreConfigSections();

        var entries = container.Resolve<List<ConfigRegistrationData>>();

        Assert.Contains(entries, entry => entry.SectionName == "storage" && entry.ConfigType == typeof(StorageConfig));
        Assert.Contains(entries, entry => entry.SectionName == "secrets" && entry.ConfigType == typeof(SecretsConfig));
        Assert.False(container.IsRegistered<StorageConfig>());
        Assert.False(container.IsRegistered<SecretsConfig>());
    }

    [Fact]
    public void RegisterMetricsCollectionService_AddsLateServiceRegistrationData()
    {
        using var container = new Container();

        container.RegisterMetricsCollectionService();

        var entry = Assert.Single(
            container.Resolve<List<ServiceRegistrationData>>(),
            registration => registration.ServiceType == typeof(IMetricsCollectionService)
        );

        Assert.Equal(typeof(MetricsCollectionService), entry.ImplementationType);
        Assert.Equal(1000, entry.Priority);
    }

    [Fact]
    public void RegisterCoreServices_RegistersMetricsCollectionService()
    {
        using var temp = new TempDirectory();
        using var container = new Container();

        container.RegisterCoreServices("app", temp.Path);

        Assert.True(container.IsRegistered<IMetricsCollectionService>());
    }

    [Fact]
    public void RegisterCoreServices_RegistersSecretServices()
    {
        using var temp = new TempDirectory();
        using var container = new Container();

        container.RegisterCoreServices("app", temp.Path);

        // Storage is opt-in (AddFileStorage); core no longer registers it.
        Assert.False(container.IsRegistered<IStorageService>());
        Assert.False(container.IsRegistered<IObjectStorageService>());
        Assert.True(container.IsRegistered<ISecretProtector>());
        Assert.True(container.IsRegistered<ISecretStore>());
    }

    [Fact]
    public void RegisterSecretServices_RegistersSecretProtectorAndStore()
    {
        using var container = new Container();

        container.RegisterSecretServices();

        Assert.True(container.IsRegistered<ISecretProtector>());
        Assert.True(container.IsRegistered<ISecretStore>());
    }
}
