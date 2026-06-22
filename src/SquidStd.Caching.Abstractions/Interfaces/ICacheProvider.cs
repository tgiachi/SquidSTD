using SquidStd.Abstractions.Interfaces.Services;

namespace SquidStd.Caching.Abstractions.Interfaces;

/// <summary>
/// Byte-level cache backend. Implemented by each provider (in-memory, Redis, ...).
/// </summary>
public interface ICacheProvider : ISquidStdService
{
    /// <summary>Gets the raw value for a key, or <c>null</c> when absent.</summary>
    Task<ReadOnlyMemory<byte>?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores a raw value with an optional time-to-live.</summary>
    Task SetAsync(string key, ReadOnlyMemory<byte> value, TimeSpan? ttl, CancellationToken cancellationToken = default);

    /// <summary>Removes a key; returns whether it existed.</summary>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a key exists.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
