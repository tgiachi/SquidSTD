namespace SquidStd.Caching.Abstractions.Interfaces;

/// <summary>
/// Sink for cache instrumentation events.
/// </summary>
public interface ICacheMetrics
{
    /// <summary>Records a cache hit.</summary>
    void OnHit(string key);

    /// <summary>Records a cache miss.</summary>
    void OnMiss(string key);

    /// <summary>Records a value being stored.</summary>
    void OnSet(string key);

    /// <summary>Records a key being removed.</summary>
    void OnRemove(string key);
}
