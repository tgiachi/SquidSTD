using DryIoc;
using Serilog;
using Serilog.Events;
using SquidStd.Abstractions.Data.Internal.Services;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Core.Interfaces.Bootstrap;
using SquidStd.Core.Interfaces.Config;
using SquidStd.Core.Types;
using SquidStd.Services.Core.Extensions;

namespace SquidStd.Services.Core.Services;

/// <summary>
/// Default SquidStd bootstrapper and service lifecycle orchestrator.
/// </summary>
public sealed class SquidStdBootstrap : ISquidStdBootstrap
{
    private readonly Lock _syncRoot = new();
    private readonly List<ISquidStdService> _startedServices = [];
    private int _disposed;
    private bool _loggerConfigured;
    private BootstrapState _state;

    /// <summary>
    /// Initializes a bootstrapper with default options.
    /// </summary>
    public SquidStdBootstrap()
        : this(new SquidStdOptions())
    {
    }

    /// <summary>
    /// Initializes a bootstrapper with the specified options.
    /// </summary>
    /// <param name="options">Bootstrap options used to register core services.</param>
    public SquidStdBootstrap(SquidStdOptions options)
        : this(options, new Container())
    {
    }

    private SquidStdBootstrap(SquidStdOptions options, IContainer container)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConfigName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.RootDirectory);

        Options = options;
        Container = container;

        Container.RegisterInstance<ISquidStdBootstrap>(this, IfAlreadyRegistered.Replace);
        Container.RegisterInstance(this, IfAlreadyRegistered.Replace);
        Container.RegisterInstance(Options, IfAlreadyRegistered.Replace);
        Container.RegisterCoreServices(Options.ConfigName, Options.RootDirectory);
    }

    /// <inheritdoc />
    public SquidStdOptions Options { get; }

    /// <inheritdoc />
    public IContainer Container { get; }

    /// <summary>
    /// Creates a bootstrapper with default options.
    /// </summary>
    /// <returns>The created bootstrapper.</returns>
    public static SquidStdBootstrap Create()
        => new();

    /// <summary>
    /// Creates a bootstrapper with the specified options.
    /// </summary>
    /// <param name="options">Bootstrap options used to register core services.</param>
    /// <returns>The created bootstrapper.</returns>
    public static SquidStdBootstrap Create(SquidStdOptions options)
        => new(options);

    /// <inheritdoc />
    public ISquidStdBootstrap ConfigureService(Func<IContainer, IContainer> configure)
        => ConfigureServices(configure);

    /// <inheritdoc />
    public ISquidStdBootstrap ConfigureServices(Func<IContainer, IContainer> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ThrowIfDisposed();

        lock (_syncRoot)
        {
            if (_state != BootstrapState.Created)
            {
                throw new InvalidOperationException("Services cannot be configured after bootstrap start.");
            }
        }

        var configuredContainer = configure(Container);

        if (!ReferenceEquals(configuredContainer, Container))
        {
            throw new InvalidOperationException("ConfigureServices must return the bootstrap container instance.");
        }

        return this;
    }

    /// <inheritdoc />
    public TService Resolve<TService>()
    {
        ThrowIfDisposed();

        return Container.Resolve<TService>();
    }

    /// <inheritdoc />
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        MarkStarting();

        try
        {
            var registrations = GetServiceRegistrations();
            var startedInstances = new HashSet<ISquidStdService>(ReferenceEqualityComparer.Instance);

            for (var i = 0; i < registrations.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var registration = registrations[i];
                var instance = Container.Resolve(registration.ServiceType);

                if (instance is not ISquidStdService service || !startedInstances.Add(service))
                {
                    continue;
                }

                await service.StartAsync(cancellationToken);
                _startedServices.Add(service);

                if (service is IConfigManagerService)
                {
                    ConfigureLogger();
                }
            }
        }
        catch
        {
            MarkCreated();

            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (!MarkStopping())
        {
            return;
        }

        try
        {
            for (var i = _startedServices.Count - 1; i >= 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _startedServices[i].StopAsync(cancellationToken);
            }
        }
        finally
        {
            _startedServices.Clear();
        }
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await StopAsync(CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await StopAsync(CancellationToken.None);
        }
        finally
        {
            if (_loggerConfigured)
            {
                Log.CloseAndFlush();
            }

            Container.Dispose();
        }
    }

    private void ConfigureLogger()
    {
        if (!Container.IsRegistered<SquidStdLoggerOptions>())
        {
            return;
        }

        var options = Container.Resolve<SquidStdLoggerOptions>();
        var loggerConfiguration = new LoggerConfiguration();

        if (options.MinimumLevel != LogLevelType.None)
        {
            var minimumLevel = MapLogLevel(options.MinimumLevel);
            loggerConfiguration.MinimumLevel.Is(minimumLevel);

            if (options.EnableConsole)
            {
                loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: minimumLevel);
            }

            if (options.EnableFile)
            {
                loggerConfiguration.WriteTo.File(
                    ResolveLogPath(options),
                    rollingInterval: MapRollingInterval(options.RollingInterval),
                    restrictedToMinimumLevel: minimumLevel
                );
            }
        }

        var logger = loggerConfiguration.CreateLogger();
        Log.Logger = logger;
        Container.RegisterInstance<Serilog.ILogger>(logger, IfAlreadyRegistered.Replace);
        _loggerConfigured = true;
    }

    private ServiceRegistrationData[] GetServiceRegistrations()
    {
        if (!Container.IsRegistered<List<ServiceRegistrationData>>())
        {
            return [];
        }

        return
        [
            .. Container.Resolve<List<ServiceRegistrationData>>()
                        .OrderBy(registration => registration.Priority)
                        .ThenBy(registration => registration.ServiceType.FullName, StringComparer.Ordinal)
                        .ThenBy(registration => registration.ImplementationType.FullName, StringComparer.Ordinal)
        ];
    }

    private LogEventLevel MapLogLevel(LogLevelType level)
        => level switch
        {
            LogLevelType.Trace       => LogEventLevel.Verbose,
            LogLevelType.Debug       => LogEventLevel.Debug,
            LogLevelType.Information => LogEventLevel.Information,
            LogLevelType.Warning     => LogEventLevel.Warning,
            LogLevelType.Error       => LogEventLevel.Error,
            LogLevelType.Critical    => LogEventLevel.Fatal,
            _                        => LogEventLevel.Fatal
        };

    private RollingInterval MapRollingInterval(SquidStdLogRollingIntervalType interval)
        => interval switch
        {
            SquidStdLogRollingIntervalType.Year     => RollingInterval.Year,
            SquidStdLogRollingIntervalType.Month    => RollingInterval.Month,
            SquidStdLogRollingIntervalType.Day      => RollingInterval.Day,
            SquidStdLogRollingIntervalType.Hour     => RollingInterval.Hour,
            SquidStdLogRollingIntervalType.Minute   => RollingInterval.Minute,
            SquidStdLogRollingIntervalType.Infinite => RollingInterval.Infinite,
            _                                       => RollingInterval.Day
        };

    private string ResolveLogPath(SquidStdLoggerOptions options)
    {
        var logDirectory = string.IsNullOrWhiteSpace(options.LogDirectory) ? "logs" : options.LogDirectory;
        var fileName = string.IsNullOrWhiteSpace(options.FileName) ? "squidstd-.log" : options.FileName;
        var directory = Path.IsPathRooted(logDirectory)
                            ? logDirectory
                            : Path.Combine(Options.RootDirectory, logDirectory);

        return Path.Combine(directory, fileName);
    }

    private void MarkStarting()
    {
        lock (_syncRoot)
        {
            if (_state == BootstrapState.Started)
            {
                return;
            }

            if (_state == BootstrapState.Stopped)
            {
                throw new InvalidOperationException("Bootstrap cannot be restarted after stop.");
            }

            _state = BootstrapState.Started;
        }
    }

    private void MarkCreated()
    {
        lock (_syncRoot)
        {
            _state = BootstrapState.Created;
        }
    }

    private bool MarkStopping()
    {
        lock (_syncRoot)
        {
            if (_state == BootstrapState.Stopped)
            {
                return false;
            }

            if (_state == BootstrapState.Created)
            {
                _state = BootstrapState.Stopped;

                return false;
            }

            _state = BootstrapState.Stopped;

            return true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(SquidStdBootstrap));
        }
    }

    private enum BootstrapState
    {
        Created,
        Started,
        Stopped
    }
}
