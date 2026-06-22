using SquidStd.Core.Types.Metrics;

namespace SquidStd.Core.Data.Metrics;

/// <summary>
/// Represents one collected metric value.
/// </summary>
/// <param name="Name">Metric name emitted by the provider.</param>
/// <param name="Value">Metric numeric value.</param>
/// <param name="Timestamp">Optional timestamp for the metric value.</param>
/// <param name="Tags">Optional dimensions associated with the value.</param>
/// <param name="Type">Metric semantic type.</param>
/// <param name="Help">Optional human-readable metric description.</param>
public sealed record MetricSample(
    string Name,
    double Value,
    DateTimeOffset? Timestamp = null,
    IReadOnlyDictionary<string, string>? Tags = null,
    MetricType Type = MetricType.Gauge,
    string? Help = null
);
