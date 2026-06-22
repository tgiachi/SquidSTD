using Microsoft.Extensions.Caching.Memory;
using SquidStd.Caching.Abstractions.Interfaces;

namespace SquidStd.Caching.Services;

/// <summary>
/// In-memory <see cref="ICacheProvider" /> backed by <see cref="IMemoryCache" />.
/// </summary>
public sealed class InMemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;

    public InMemoryCacheProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public Task<ReadOnlyMemory<byte>?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out byte[]? value) && value is not null)
        {
            return Task.FromResult<ReadOnlyMemory<byte>?>(new ReadOnlyMemory<byte>(value));
        }

        return Task.FromResult<ReadOnlyMemory<byte>?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();

        if (ttl is not null)
        {
            options.AbsoluteExpirationRelativeToNow = ttl;
        }

        _cache.Set(key, value.ToArray(), options);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var existed = _cache.TryGetValue(key, out _);
        _cache.Remove(key);

        return Task.FromResult(existed);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_cache.TryGetValue(key, out _));

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
