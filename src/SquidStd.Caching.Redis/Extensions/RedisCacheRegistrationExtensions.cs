using DryIoc;
using SquidStd.Caching.Abstractions.Data.Config;
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Abstractions.Services;
using SquidStd.Caching.Redis.Data.Config;
using SquidStd.Caching.Redis.Services;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Core.Json;

namespace SquidStd.Caching.Redis.Extensions;

/// <summary>
/// DryIoc registration helpers for the Redis cache provider.
/// </summary>
public static class RedisCacheRegistrationExtensions
{
    /// <summary>Registers the Redis cache from explicit options.</summary>
    public static IContainer AddRedisCache(
        this IContainer container,
        RedisCacheOptions options,
        CacheOptions? cacheOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(options);

        container.RegisterInstance(cacheOptions ?? new CacheOptions());
        container.RegisterInstance(options);

        var serializer = new JsonDataSerializer();
        container.RegisterInstance<IDataSerializer>(serializer, IfAlreadyRegistered.Keep);
        container.RegisterInstance<IDataDeserializer>(serializer, IfAlreadyRegistered.Keep);

        var metrics = new CacheMetricsProvider();
        container.RegisterInstance<ICacheMetrics>(metrics);
        container.RegisterInstance<IMetricProvider>(metrics);

        container.Register<ICacheProvider, RedisCacheProvider>(Reuse.Singleton);
        container.Register<ICacheService, CacheService>(Reuse.Singleton);

        return container;
    }

    /// <summary>Registers the Redis cache from a connection string (scheme must be "redis").</summary>
    public static IContainer AddRedisCache(this IContainer container, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(container);

        var cs = CacheConnectionString.Parse(connectionString);

        if (!string.Equals(cs.Scheme, "redis", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Expected a 'redis://' connection string but got '{cs.Scheme}://'.",
                nameof(connectionString)
            );
        }

        var host = string.IsNullOrEmpty(cs.Host) ? "localhost" : cs.Host;
        var port = cs.Port ?? 6379;
        var options = new RedisCacheOptions { Configuration = $"{host}:{port}" };

        return container.AddRedisCache(options, cs.ToCacheOptions());
    }
}
