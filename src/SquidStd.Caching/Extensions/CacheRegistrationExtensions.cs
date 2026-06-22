using DryIoc;
using Microsoft.Extensions.Caching.Memory;
using SquidStd.Caching.Abstractions.Data.Config;
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Abstractions.Services;
using SquidStd.Caching.Services;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Core.Json;

namespace SquidStd.Caching.Extensions;

/// <summary>
/// DryIoc registration helpers for the in-memory cache.
/// </summary>
public static class CacheRegistrationExtensions
{
    /// <summary>Registers the in-memory cache (provider, facade, metrics, serializer).</summary>
    public static IContainer AddInMemoryCache(this IContainer container, CacheOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.RegisterInstance(options ?? new CacheOptions());

        var serializer = new JsonDataSerializer();
        container.RegisterInstance<IDataSerializer>(serializer, IfAlreadyRegistered.Keep);
        container.RegisterInstance<IDataDeserializer>(serializer, IfAlreadyRegistered.Keep);

        container.RegisterInstance<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()), IfAlreadyRegistered.Keep);

        var metrics = new CacheMetricsProvider();
        container.RegisterInstance<ICacheMetrics>(metrics);
        container.RegisterInstance<IMetricProvider>(metrics);

        container.Register<ICacheProvider, InMemoryCacheProvider>(Reuse.Singleton);
        container.Register<ICacheService, CacheService>(Reuse.Singleton);

        return container;
    }

    /// <summary>Registers the in-memory cache from a connection string (scheme must be "memory").</summary>
    public static IContainer AddInMemoryCache(this IContainer container, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(container);

        var cs = CacheConnectionString.Parse(connectionString);

        if (!string.Equals(cs.Scheme, "memory", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Expected a 'memory://' connection string but got '{cs.Scheme}://'.",
                nameof(connectionString)
            );
        }

        return container.AddInMemoryCache(cs.ToCacheOptions());
    }
}
