using SquidStd.Core.Data.Metrics;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class MetricsCollectionServiceTests
{
    [Fact]
    public async Task StartAsync_CollectsMetricsFromRegisteredProviders()
    {
        using var service = new MetricsCollectionService(
            [new CountingMetricProvider("jobs", "pending.total", 7)],
            new()
            {
                IntervalMilliseconds = 10,
                LogEnabled = false
            }
        );

        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => service.GetAllMetrics().ContainsKey("jobs.pending.total"));

        var metrics = service.GetAllMetrics();
        var sample = metrics["jobs.pending.total"];

        Assert.Equal("pending.total", sample.Name);
        Assert.Equal(7, sample.Value);
        Assert.NotNull(sample.Timestamp);
    }

    [Fact]
    public async Task StartAsync_WhenProviderThrows_ContinuesCollectingOtherProviders()
    {
        using var service = new MetricsCollectionService(
            [
                new ThrowingMetricProvider("broken"),
                new CountingMetricProvider("timer", "callbacks.total", 3)
            ],
            new()
            {
                IntervalMilliseconds = 10,
                LogEnabled = false
            }
        );

        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => service.GetAllMetrics().ContainsKey("timer.callbacks.total"));

        var metrics = service.GetAllMetrics();

        Assert.Contains("timer.callbacks.total", metrics.Keys);
        Assert.DoesNotContain("broken", metrics.Keys);
    }

    [Fact]
    public async Task StopAsync_StopsCollectionLoop()
    {
        var provider = new CountingMetricProvider("bus", "dispatch.total", 1);
        using var service = new MetricsCollectionService(
            [provider],
            new()
            {
                IntervalMilliseconds = 10,
                LogEnabled = false
            }
        );

        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => provider.CollectionCount > 0);
        await service.StopAsync(CancellationToken.None);
        var countAfterStop = provider.CollectionCount;

        await Task.Delay(50);

        Assert.Equal(countAfterStop, provider.CollectionCount);
    }

    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotCollectMetrics()
    {
        var provider = new CountingMetricProvider("jobs", "pending.total", 7);
        using var service = new MetricsCollectionService(
            [provider],
            new()
            {
                Enabled = false,
                IntervalMilliseconds = 10,
                LogEnabled = false
            }
        );

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(30);

        Assert.Empty(service.GetAllMetrics());
        Assert.Equal(0, provider.CollectionCount);
    }

    [Fact]
    public void GetSnapshot_WhenNotStarted_ReturnsEmptySnapshot()
    {
        using var service = new MetricsCollectionService([], new());

        var snapshot = service.GetSnapshot();

        Assert.Equal(DateTimeOffset.MinValue, snapshot.CollectedAt);
        Assert.Empty(snapshot.Metrics);
    }

    [Fact]
    public async Task GetStatus_ReturnsLatestMetricsSnapshot()
    {
        using var service = new MetricsCollectionService(
            [new CountingMetricProvider("jobs", "completed.total", 11)],
            new()
            {
                IntervalMilliseconds = 10,
                LogEnabled = false
            }
        );
        IMetricsCollectionService metrics = service;

        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => metrics.GetStatus().Metrics.ContainsKey("jobs.completed.total"));

        var status = metrics.GetStatus();

        Assert.NotEqual(DateTimeOffset.MinValue, status.CollectedAt);
        Assert.Equal(11, status.Metrics["jobs.completed.total"].Value);
    }

    [Fact]
    public async Task StartAsync_PublishesMetricsCollectedEventThroughEventBus()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var syncListener = new MetricsCollectedSyncListener();
        var asyncListener = new MetricsCollectedAsyncListener();
        bus.RegisterListener(syncListener);
        bus.RegisterAsyncListener(asyncListener);
        using var service = new MetricsCollectionService(
            [new CountingMetricProvider("events", "published.total", 5)],
            new()
            {
                IntervalMilliseconds = 1000,
                LogEnabled = false
            },
            bus
        );

        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => syncListener.LastEvent is not null && asyncListener.LastEvent is not null);
        await service.StopAsync(CancellationToken.None);

        Assert.NotNull(syncListener.LastEvent);
        Assert.NotNull(asyncListener.LastEvent);
        Assert.Same(syncListener.LastEvent, asyncListener.LastEvent);
        Assert.Same(service.GetStatus(), syncListener.LastEvent.Snapshot);
        Assert.Equal(5, syncListener.LastEvent.Snapshot.Metrics["events.published.total"].Value);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail("Condition was not met before timeout.");
    }

    private sealed class CountingMetricProvider : IMetricProvider
    {
        private readonly string _metricName;
        private readonly double _value;
        private int _collectionCount;

        public CountingMetricProvider(string providerName, string metricName, double value)
        {
            ProviderName = providerName;
            _metricName = metricName;
            _value = value;
        }

        public int CollectionCount => Volatile.Read(ref _collectionCount);

        public string ProviderName { get; }

        public ValueTask<IReadOnlyList<MetricSample>> CollectAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _collectionCount);

            return ValueTask.FromResult<IReadOnlyList<MetricSample>>([new(_metricName, _value)]);
        }
    }

    private sealed class ThrowingMetricProvider : IMetricProvider
    {
        public ThrowingMetricProvider(string providerName)
        {
            ProviderName = providerName;
        }

        public string ProviderName { get; }

        public ValueTask<IReadOnlyList<MetricSample>> CollectAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Synthetic test failure.");
    }

    private sealed class MetricsCollectedSyncListener : ISyncEventListener<MetricsCollectedEvent>
    {
        public MetricsCollectedEvent? LastEvent { get; private set; }

        public void Handle(MetricsCollectedEvent eventData)
            => LastEvent = eventData;
    }

    private sealed class MetricsCollectedAsyncListener : IAsyncEventListener<MetricsCollectedEvent>
    {
        public MetricsCollectedEvent? LastEvent { get; private set; }

        public Task HandleAsync(MetricsCollectedEvent eventData, CancellationToken cancellationToken)
        {
            LastEvent = eventData;

            return Task.CompletedTask;
        }
    }
}
