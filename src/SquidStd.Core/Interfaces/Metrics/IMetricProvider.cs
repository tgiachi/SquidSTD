using SquidStd.Core.Data.Metrics;

namespace SquidStd.Core.Interfaces.Metrics;

/// <summary>
/// Provides metric samples for one subsystem domain.
/// </summary>
public interface IMetricProvider
{
    /// <summary>
    /// Gets the unique provider name used as metric name prefix.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Collects the current metric samples for this provider.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel collection.</param>
    /// <returns>The collected metric samples.</returns>
    ValueTask<IReadOnlyList<MetricSample>> CollectAsync(CancellationToken cancellationToken = default);
}
