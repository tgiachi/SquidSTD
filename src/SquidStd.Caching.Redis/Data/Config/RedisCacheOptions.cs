namespace SquidStd.Caching.Redis.Data.Config;

/// <summary>
/// Connection options for the Redis cache provider.
/// </summary>
public sealed class RedisCacheOptions
{
    /// <summary>StackExchange.Redis configuration string. Default "localhost:6379".</summary>
    public string Configuration { get; init; } = "localhost:6379";
}
