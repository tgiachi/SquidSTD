using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Data.Metrics;
using SquidStd.Core.Extensions.Logger;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Types;

namespace SquidStd.Services.Core.Services;

/// <summary>
///     Periodically collects metrics from registered providers and stores the latest snapshot.
/// </summary>
public sealed class MetricsCollectionService : IMetricsCollectionService, ISquidStdService, IDisposable
{
    private readonly MetricsConfig _config;
    private readonly IEventBus? _eventBus;
    private readonly ILogger _logger = Log.ForContext<MetricsCollectionService>();
    private readonly ILogger _metricsLogger = Log.ForContext<MetricsCollectionService>().ForContext("MetricsData", true);
    private readonly IReadOnlyList<IMetricProvider> _providers;
    private readonly Lock _syncRoot = new();
    private Task _collectionTask = Task.CompletedTask;
    private int _disposed;
    private CancellationTokenSource _lifetimeCts = new();

    private MetricsSnapshot _snapshot = new(
        DateTimeOffset.MinValue,
        new Dictionary<string, MetricSample>(StringComparer.Ordinal)
    );

    private int _started;

    /// <summary>
    ///     Initializes the metrics collection service.
    /// </summary>
    /// <param name="providers">Metric providers to collect from.</param>
    /// <param name="config">Metrics collection configuration.</param>
    /// <param name="eventBus">Optional event bus used to publish metrics collection events.</param>
    public MetricsCollectionService(IEnumerable<IMetricProvider> providers, MetricsConfig config, IEventBus? eventBus = null)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(config);

        _providers = [.. providers];
        _config = config;
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, MetricSample> GetAllMetrics()
    {
        lock (_syncRoot)
        {
            return _snapshot.Metrics;
        }
    }

    /// <inheritdoc />
    public MetricsSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _snapshot;
        }
    }

    /// <inheritdoc />
    public MetricsSnapshot GetStatus()
    {
        return GetSnapshot();
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (!_config.Enabled)
        {
            return ValueTask.CompletedTask;
        }

        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            return ValueTask.CompletedTask;
        }

        if (_lifetimeCts.IsCancellationRequested)
        {
            _lifetimeCts.Dispose();
            _lifetimeCts = new CancellationTokenSource();
        }

        _collectionTask = Task.Run(() => RunCollectionLoopAsync(_lifetimeCts.Token), _lifetimeCts.Token);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _started, 0, 1) != 1)
        {
            return;
        }

        await _lifetimeCts.CancelAsync();

        try
        {
            await _collectionTask;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task CollectOnceAsync(CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, MetricSample>(StringComparer.Ordinal);
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < _providers.Count; i++)
        {
            var provider = _providers[i];

            try
            {
                var samples = await provider.CollectAsync(cancellationToken);

                for (var sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
                {
                    var sample = samples[sampleIndex];
                    var key = CreateMetricKey(provider.ProviderName, sample.Name);
                    values[key] = sample with { Timestamp = sample.Timestamp ?? now };
                }

                LogProviderCollection(provider, samples.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Metrics provider collection failed for {ProviderName}", provider.ProviderName);
            }
        }

        var snapshot = new MetricsSnapshot(now, values);

        lock (_syncRoot)
        {
            _snapshot = snapshot;
        }

        await PublishMetricsCollectedAsync(snapshot, cancellationToken);
    }

    private static string CreateMetricKey(string providerName, string metricName)
    {
        return string.IsNullOrWhiteSpace(providerName) ? metricName :
            string.IsNullOrWhiteSpace(metricName) ? providerName : providerName + "." + metricName;
    }

    private void LogProviderCollection(IMetricProvider provider, int metricCount)
    {
        if (!_config.LogEnabled || _config.LogLevel == LogLevelType.None)
        {
            return;
        }

        _metricsLogger.Write(
            _config.LogLevel.ToSerilogLogLevel(),
            "Collected {MetricCount} metrics from provider {ProviderName}",
            metricCount,
            provider.ProviderName
        );
    }

    private async Task PublishMetricsCollectedAsync(MetricsSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (_eventBus is null)
        {
            return;
        }

        var eventData = new MetricsCollectedEvent(snapshot);

        try
        {
            await _eventBus.PublishAsync(eventData, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Metrics collected event dispatch failed");
        }
    }

    private async Task RunCollectionLoopAsync(CancellationToken cancellationToken)
    {
        await CollectOnceAsync(cancellationToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(1, _config.IntervalMilliseconds)));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await CollectOnceAsync(cancellationToken);
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(MetricsCollectionService));
        }
    }

    /// <summary>
    ///     Releases metrics collection resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (Volatile.Read(ref _started) != 0)
        {
            _lifetimeCts.Cancel();

            try
            {
                _collectionTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
        }

        _lifetimeCts.Dispose();
    }
}
