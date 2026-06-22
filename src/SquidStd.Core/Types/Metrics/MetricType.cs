namespace SquidStd.Core.Types.Metrics;

/// <summary>
/// Defines the semantic type of a metric sample.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// A value that can go up or down.
    /// </summary>
    Gauge,

    /// <summary>
    /// A cumulative value that only increases.
    /// </summary>
    Counter,

    /// <summary>
    /// A value that represents a distribution bucket or aggregate.
    /// </summary>
    Histogram
}
