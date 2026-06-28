using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Types.Metrics;

namespace SquidStd.Telemetry.OpenTelemetry.Services;

/// <summary>
///     Bridges the SquidStd metrics snapshot (<see cref="IMetricsCollectionService" />) to OpenTelemetry
///     observable instruments: counters become observable counters, everything else an observable gauge,
///     each callback reading the latest snapshot value for its metric name.
/// </summary>
public sealed class MetricsSnapshotBridge : IDisposable
{
    /// <summary>The meter name registered on the OpenTelemetry MeterProvider.</summary>
    public const string MeterName = "SquidStd.Metrics";

    private readonly Meter _meter;

    private readonly IMetricsCollectionService _metrics;
    private readonly ConcurrentDictionary<string, byte> _registered = new(StringComparer.Ordinal);
    private int _disposed;

    public MetricsSnapshotBridge(IMetricsCollectionService metrics)
    {
        _metrics = metrics;
        _meter = new Meter(MeterName);
        EnsureInstruments();
    }

    /// <summary>Creates observable instruments for any snapshot metric names not yet registered.</summary>
    public void EnsureInstruments()
    {
        foreach (var (name, sample) in _metrics.GetSnapshot().Metrics)
        {
            if (!_registered.TryAdd(name, 0))
            {
                continue;
            }

            if (sample.Type == MetricType.Counter)
            {
                _meter.CreateObservableCounter(name, () => Observe(name), description: sample.Help);
            }
            else
            {
                _meter.CreateObservableGauge(name, () => Observe(name), description: sample.Help);
            }
        }
    }

    private IEnumerable<Measurement<double>> Observe(string name)
    {
        if (!_metrics.GetSnapshot().Metrics.TryGetValue(name, out var sample))
        {
            return [];
        }

        var tags = sample.Tags is { Count: > 0 }
            ? sample.Tags.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)).ToArray()
            : [];

        return [new Measurement<double>(sample.Value, tags)];
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _meter.Dispose();
    }
}
