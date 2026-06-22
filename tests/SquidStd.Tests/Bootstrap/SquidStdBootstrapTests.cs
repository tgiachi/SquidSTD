using DryIoc;
using Serilog;
using SquidStd.Abstractions.Data.Internal.Services;
using SquidStd.Abstractions.Extensions.Container;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Core.Interfaces.Bootstrap;
using SquidStd.Core.Interfaces.Config;
using SquidStd.Core.Types;
using SquidStd.Services.Core.Services;
using SquidStd.Services.Core.Services.Bootstrap;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Bootstrap;

[Collection(SerilogEventSinkCollection.Name)]
public class SquidStdBootstrapTests
{
    [Fact]
    public async Task Create_RegistersBootstrapAndDefaultServices()
    {
        using var temp = new TempDirectory();
        await using var bootstrap = SquidStdBootstrap.Create(
            new SquidStdOptions { ConfigName = "app", RootDirectory = temp.Path }
        );

        var resolved = bootstrap.Resolve<ISquidStdBootstrap>();
        var configManager = bootstrap.Resolve<IConfigManagerService>();

        Assert.Same(bootstrap, resolved);
        Assert.Equal(temp.Path, bootstrap.Options.RootDirectory);
        Assert.Equal(Path.Combine(temp.Path, "app.yaml"), configManager.ConfigPath);
    }

    [Fact]
    public async Task Create_WithContainer_UsesProvidedContainer()
    {
        using var temp = new TempDirectory();
        var container = new Container();

        await using var bootstrap = SquidStdBootstrap.Create(
            new()
            {
                ConfigName = "app",
                RootDirectory = temp.Path
            },
            container
        );

        Assert.Same(container, bootstrap.Container);
        Assert.Same(bootstrap, container.Resolve<ISquidStdBootstrap>());
        Assert.Same(bootstrap.Options, container.Resolve<SquidStdOptions>());

        container.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_WithProvidedContainer_DoesNotDisposeContainer()
    {
        using var temp = new TempDirectory();
        var container = new Container();

        await using (SquidStdBootstrap.Create(
                         new()
                         {
                             ConfigName = "app",
                             RootDirectory = temp.Path
                         },
                         container
                     ))
        {
        }

        container.RegisterInstance("still-open");

        Assert.Equal("still-open", container.Resolve<string>());

        container.Dispose();
    }

    [Fact]
    public async Task StartAsync_LoadsConfigBeforeResolvingRegisteredServices()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            temp.Combine("app.yaml"),
            """
            logger:
              MinimumLevel: Warning
              EnableConsole: false
            """
        );
        var events = new List<string>();
        await using var bootstrap = SquidStdBootstrap.Create(
            new SquidStdOptions { ConfigName = "app", RootDirectory = temp.Path }
        );

        bootstrap.ConfigureServices(
            container =>
            {
                container.RegisterInstance(events);
                container.Register<ConfigConsumerService>(Reuse.Singleton);
                container.AddToRegisterTypedList(
                    new ServiceRegistrationData(
                        typeof(ConfigConsumerService),
                        typeof(ConfigConsumerService),
                        -10
                    )
                );

                return container;
            }
        );

        await bootstrap.StartAsync(CancellationToken.None);
        await bootstrap.StopAsync(CancellationToken.None);

        Assert.Contains("config:Warning", events);
    }

    [Fact]
    public async Task StartAsync_OrdersServicesByPriorityAndStopAsync_ReversesOrder()
    {
        using var temp = new TempDirectory();
        var events = new List<string>();
        await using var bootstrap = SquidStdBootstrap.Create(
            new SquidStdOptions { ConfigName = "app", RootDirectory = temp.Path }
        );

        bootstrap.ConfigureServices(
            container =>
            {
                container.RegisterInstance(events);
                container.Register<EarlyTrackedService>(Reuse.Singleton);
                container.Register<LateTrackedService>(Reuse.Singleton);
                container.AddToRegisterTypedList(
                    new ServiceRegistrationData(
                        typeof(EarlyTrackedService),
                        typeof(EarlyTrackedService),
                        -20
                    )
                );
                container.AddToRegisterTypedList(
                    new ServiceRegistrationData(typeof(LateTrackedService), typeof(LateTrackedService), 40)
                );

                return container;
            }
        );

        await bootstrap.StartAsync(CancellationToken.None);
        await bootstrap.StopAsync(CancellationToken.None);

        Assert.True(events.IndexOf("early:start") < events.IndexOf("late:start"));
        Assert.True(events.IndexOf("late:stop") < events.IndexOf("early:stop"));
    }

    [Fact]
    public async Task RunAsync_StartsUntilCancellationThenStops()
    {
        using var temp = new TempDirectory();
        using var cancellation = new CancellationTokenSource();
        var state = new RunTrackedState();
        await using var bootstrap = SquidStdBootstrap.Create(
            new SquidStdOptions { ConfigName = "app", RootDirectory = temp.Path }
        );

        bootstrap.ConfigureService(
            container =>
            {
                container.RegisterInstance(state);
                container.Register<RunTrackedService>(Reuse.Singleton);
                container.AddToRegisterTypedList(
                    new ServiceRegistrationData(
                        typeof(RunTrackedService),
                        typeof(RunTrackedService),
                        -20
                    )
                );

                return container;
            }
        );

        var runTask = bootstrap.RunAsync(cancellation.Token);

        await state.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await cancellation.CancelAsync();
        await runTask.WaitAsync(TimeSpan.FromSeconds(3));
        await state.Stopped.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.True(state.Stopped.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task StartAsync_ConfiguresFileSinkFromLoggerOptions()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            temp.Combine("app.yaml"),
            """
            logger:
              MinimumLevel: Information
              EnableConsole: false
              EnableFile: true
              LogDirectory: logs
              FileName: app.log
              RollingInterval: Infinite
            """
        );
        await using var bootstrap = SquidStdBootstrap.Create(
            new SquidStdOptions { ConfigName = "app", RootDirectory = temp.Path }
        );

        await bootstrap.StartAsync(CancellationToken.None);
        await bootstrap.StopAsync(CancellationToken.None);
        Log.CloseAndFlush();

        var logPath = temp.Combine(Path.Combine("logs", "app.log"));
        var content = File.ReadAllText(logPath);

        Assert.Contains("JobSystemService started", content);
    }

    private sealed class ConfigConsumerService(SquidStdLoggerOptions options, List<string> events) : ISquidStdService
    {
        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            events.Add($"config:{options.MinimumLevel}");

            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class EarlyTrackedService(List<string> events) : ISquidStdService
    {
        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            events.Add("early:start");

            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            events.Add("early:stop");

            return ValueTask.CompletedTask;
        }
    }

    private sealed class LateTrackedService(List<string> events) : ISquidStdService
    {
        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            events.Add("late:start");

            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            events.Add("late:stop");

            return ValueTask.CompletedTask;
        }
    }

    private sealed class RunTrackedState
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class RunTrackedService(RunTrackedState state) : ISquidStdService
    {
        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            state.Started.SetResult();

            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            state.Stopped.SetResult();

            return ValueTask.CompletedTask;
        }
    }
}
