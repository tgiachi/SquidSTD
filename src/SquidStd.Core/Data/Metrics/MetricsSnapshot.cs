namespace SquidStd.Core.Data.Metrics;

/// <summary>
///     Stores the last collected metrics batch.
/// </summary>
public sealed class MetricsSnapshot
{
    /// <summary>
    ///     Gets the timestamp when the snapshot was collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; }

    /// <summary>
    ///     Gets metrics keyed by their flattened metric name.
    /// </summary>
    public IReadOnlyDictionary<string, MetricSample> Metrics { get; }

    /// <summary>
    ///     Initializes a metrics snapshot.
    /// </summary>
    /// <param name="collectedAt">Timestamp when the snapshot was collected.</param>
    /// <param name="metrics">Metrics keyed by their flattened metric name.</param>
    public MetricsSnapshot(DateTimeOffset collectedAt, IReadOnlyDictionary<string, MetricSample> metrics)
    {
        CollectedAt = collectedAt;
        Metrics = metrics;
    }
}
