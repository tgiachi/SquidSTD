using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Redis.Data.Config;
using StackExchange.Redis;

namespace SquidStd.Caching.Redis.Services;

/// <summary>
/// Redis <see cref="ICacheProvider" /> backed by a StackExchange.Redis connection multiplexer.
/// </summary>
public sealed class RedisCacheProvider : ICacheProvider, IAsyncDisposable
{
    private readonly RedisCacheOptions _options;
    private IConnectionMultiplexer? _connection;
    private int _disposed;

    public RedisCacheProvider(RedisCacheOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _connection = await ConnectionMultiplexer.ConnectAsync(_options.Configuration);
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
        => DisposeAsync();

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(key);

        if (value.IsNull)
        {
            return null;
        }

        return new ReadOnlyMemory<byte>((byte[])value!);
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl, CancellationToken cancellationToken = default)
    {
        var expiry = ttl is null ? Expiration.Default : new Expiration(ttl.Value);
        await Database.StringSetAsync(key, value.ToArray(), expiry);
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Database.KeyDeleteAsync(key);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Database.KeyExistsAsync(key);

    private IDatabase Database
        => (_connection ?? throw new InvalidOperationException("Provider not started.")).GetDatabase();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}
