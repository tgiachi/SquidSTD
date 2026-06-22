using SquidStd.Core.Data.Metrics;

namespace SquidStd.Core.Interfaces.Metrics;

/// <summary>
/// Exposes the latest metrics snapshot collected from registered providers.
/// </summary>
public interface IMetricsCollectionService
{
    /// <summary>
    /// Gets all metrics from the latest snapshot.
    /// </summary>
    /// <returns>Metrics keyed by their flattened metric name.</returns>
    IReadOnlyDictionary<string, MetricSample> GetAllMetrics();

    /// <summary>
    /// Gets the current metrics status.
    /// </summary>
    /// <returns>The current metrics snapshot.</returns>
    MetricsSnapshot GetStatus();

    /// <summary>
    /// Gets the latest collected metrics snapshot.
    /// </summary>
    /// <returns>The current metrics snapshot.</returns>
    MetricsSnapshot GetSnapshot();
}
