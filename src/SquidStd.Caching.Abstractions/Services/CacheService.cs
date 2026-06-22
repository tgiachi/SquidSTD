using SquidStd.Caching.Abstractions.Data.Config;
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Core.Interfaces.Serialization;

namespace SquidStd.Caching.Abstractions.Services;

/// <summary>
/// Typed cache-aside facade: serializes values, applies the key prefix and default TTL, and
/// implements <see cref="GetOrSetAsync{T}" /> once over any <see cref="ICacheProvider" />.
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly ICacheProvider _provider;
    private readonly IDataSerializer _serializer;
    private readonly IDataDeserializer _deserializer;
    private readonly ICacheMetrics _metrics;
    private readonly TimeSpan? _defaultTtl;
    private readonly string _keyPrefix;

    public CacheService(
        ICacheProvider provider,
        IDataSerializer serializer,
        IDataDeserializer deserializer,
        CacheOptions options,
        ICacheMetrics? metrics = null
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        _provider = provider;
        _serializer = serializer;
        _deserializer = deserializer;
        _metrics = metrics ?? NoOpCacheMetrics.Instance;
        _defaultTtl = options.DefaultTtl;
        _keyPrefix = options.KeyPrefix;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _provider.GetAsync(Prefixed(key), cancellationToken);

        if (!bytes.HasValue)
        {
            _metrics.OnMiss(key);

            return default;
        }

        _metrics.OnHit(key);

        return _deserializer.Deserialize<T>(bytes.Value);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var bytes = _serializer.Serialize(value);
        await _provider.SetAsync(Prefixed(key), bytes, ttl ?? _defaultTtl, cancellationToken);
        _metrics.OnSet(key);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var removed = await _provider.RemoveAsync(Prefixed(key), cancellationToken);

        if (removed)
        {
            _metrics.OnRemove(key);
        }

        return removed;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => _provider.ExistsAsync(Prefixed(key), cancellationToken);

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(factory);

        var bytes = await _provider.GetAsync(Prefixed(key), cancellationToken);

        if (bytes.HasValue)
        {
            _metrics.OnHit(key);

            return _deserializer.Deserialize<T>(bytes.Value);
        }

        _metrics.OnMiss(key);
        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, cancellationToken);

        return value;
    }

    private string Prefixed(string key)
        => _keyPrefix.Length == 0 ? key : _keyPrefix + key;
}
