namespace SquidStd.Caching.Abstractions.Interfaces;

/// <summary>
/// Typed cache-aside facade over an <see cref="ICacheProvider" />.
/// </summary>
public interface ICacheService
{
    /// <summary>Gets a typed value, or <c>default</c> when absent.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores a typed value with an optional time-to-live (falls back to the default TTL).</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>Removes a key; returns whether it existed.</summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a key exists.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns the cached value, or computes, stores and returns it on a miss.</summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default
    );
}
