using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Core.Data.Metrics;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Types.Metrics;

namespace SquidStd.Caching.Abstractions.Services;

/// <summary>
///     Accumulates aggregate cache metrics and exposes them to the metrics collection system.
/// </summary>
public sealed class CacheMetricsProvider : ICacheMetrics, IMetricProvider
{
    private long _hits;
    private long _misses;
    private long _removes;
    private long _sets;

    /// <inheritdoc />
    public string ProviderName => "cache";

    /// <inheritdoc />
    public void OnHit(string key)
    {
        Interlocked.Increment(ref _hits);
    }

    /// <inheritdoc />
    public void OnMiss(string key)
    {
        Interlocked.Increment(ref _misses);
    }

    /// <inheritdoc />
    public void OnRemove(string key)
    {
        Interlocked.Increment(ref _removes);
    }

    /// <inheritdoc />
    public void OnSet(string key)
    {
        Interlocked.Increment(ref _sets);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<MetricSample>> CollectAsync(CancellationToken cancellationToken = default)
    {
        var hits = Interlocked.Read(ref _hits);
        var misses = Interlocked.Read(ref _misses);
        var total = hits + misses;
        var hitRatio = total == 0 ? 0d : (double)hits / total;

        var samples = new List<MetricSample>
        {
            new("hits", hits, Type: MetricType.Counter),
            new("misses", misses, Type: MetricType.Counter),
            new("sets", Interlocked.Read(ref _sets), Type: MetricType.Counter),
            new("removes", Interlocked.Read(ref _removes), Type: MetricType.Counter),
            new("hit_ratio", hitRatio)
        };

        return ValueTask.FromResult<IReadOnlyList<MetricSample>>(samples);
    }
}
