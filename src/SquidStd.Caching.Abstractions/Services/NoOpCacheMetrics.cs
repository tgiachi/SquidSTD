using SquidStd.Caching.Abstractions.Interfaces;

namespace SquidStd.Caching.Abstractions.Services;

/// <summary>
/// Metrics sink that ignores all events. Used when no metrics are configured.
/// </summary>
public sealed class NoOpCacheMetrics : ICacheMetrics
{
    /// <summary>Shared instance.</summary>
    public static NoOpCacheMetrics Instance { get; } = new();

    public void OnHit(string key)
    {
    }

    public void OnMiss(string key)
    {
    }

    public void OnSet(string key)
    {
    }

    public void OnRemove(string key)
    {
    }
}
