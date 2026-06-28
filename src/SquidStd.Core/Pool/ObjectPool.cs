using System.Collections.Concurrent;

namespace SquidStd.Core.Pool;

/// <summary>
///     Thread-safe, non-blocking object pool. <see cref="Get" /> reuses a retained instance or creates a new
///     one through the factory; <see cref="Return" /> stocks the instance back (up to a bound), optionally
///     resetting it first. Instances dropped over the bound, and those still pooled on dispose, are disposed
///     when <typeparamref name="T" /> implements <see cref="IDisposable" />.
/// </summary>
/// <typeparam name="T">The pooled reference type.</typeparam>
public sealed class ObjectPool<T> : IDisposable
    where T : class
{
    private readonly ConcurrentQueue<T> _items = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _onReturn;
    private readonly int _maxRetained;
    private int _retained;
    private bool _disposed;

    /// <summary>
    ///     Gets the number of instances currently retained for reuse.
    /// </summary>
    public int Count => Volatile.Read(ref _retained);

    /// <summary>
    ///     Initializes the pool.
    /// </summary>
    /// <param name="factory">Creates a new instance when the pool is empty.</param>
    /// <param name="maxRetained">Maximum number of instances kept for reuse. Defaults to 1024.</param>
    /// <param name="onReturn">Optional reset applied to an instance as it is returned.</param>
    public ObjectPool(Func<T> factory, int maxRetained = 1024, Action<T>? onReturn = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (maxRetained <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetained), "Max retained must be greater than zero.");
        }

        _factory = factory;
        _maxRetained = maxRetained;
        _onReturn = onReturn;
    }

    /// <summary>
    ///     Rents an instance from the pool, creating one through the factory when the pool is empty.
    /// </summary>
    /// <returns>A pooled or freshly created instance.</returns>
    public T Get()
    {
        if (_items.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref _retained);

            return item;
        }

        return _factory();
    }

    /// <summary>
    ///     Returns an instance to the pool. Instances beyond the retained bound are disposed instead of kept.
    /// </summary>
    /// <param name="item">The instance to return.</param>
    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _onReturn?.Invoke(item);

        if (Interlocked.Increment(ref _retained) <= _maxRetained)
        {
            _items.Enqueue(item);

            return;
        }

        Interlocked.Decrement(ref _retained);
        (item as IDisposable)?.Dispose();
    }

    /// <summary>
    ///     Disposes every retained instance that implements <see cref="IDisposable" />.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        while (_items.TryDequeue(out var item))
        {
            (item as IDisposable)?.Dispose();
        }
    }
}
