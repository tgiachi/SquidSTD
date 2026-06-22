using System.Collections.Concurrent;
using SquidStd.Caching.Abstractions.Interfaces;

namespace SquidStd.Tests.Support;

/// <summary>
/// In-memory <see cref="ICacheProvider" /> for tests. Ignores TTL expiry (records the last TTL seen).
/// </summary>
public sealed class FakeCacheProvider : ICacheProvider
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public TimeSpan? LastTtl { get; private set; }

    public Task<ReadOnlyMemory<byte>?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var value))
        {
            return Task.FromResult<ReadOnlyMemory<byte>?>(new ReadOnlyMemory<byte>(value));
        }

        return Task.FromResult<ReadOnlyMemory<byte>?>(null);
    }

    public Task SetAsync(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl, CancellationToken cancellationToken = default)
    {
        LastTtl = ttl;
        _store[key] = value.ToArray();

        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryRemove(key, out _));

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.ContainsKey(key));

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
