namespace SquidStd.Caching.Abstractions.Data.Config;

/// <summary>
/// Options shared by all cache providers.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>Default time-to-live applied when a set call passes no TTL. <c>null</c> means no expiry.</summary>
    public TimeSpan? DefaultTtl { get; set; }

    /// <summary>Prefix prepended to every key. Default empty.</summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
