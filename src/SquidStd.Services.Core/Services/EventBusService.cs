using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;
using SquidStd.Core.Data.Events;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Services.Core.Services.Internal;

namespace SquidStd.Services.Core.Services;

/// <summary>
///     In-process event bus with parallel per-listener dispatch, catch-all listeners,
///     fault isolation, and slow-listener telemetry.
/// </summary>
public sealed class EventBusService : IEventBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, List<object>> _listeners = new();
    private readonly ILogger _logger = Log.ForContext<EventBusService>();
    private readonly TimeSpan _slowListenerThreshold;
    private bool _disposed;

    /// <summary>
    ///     Initializes the event bus with default options.
    /// </summary>
    public EventBusService()
        : this(new EventBusOptions())
    {
    }

    /// <summary>
    ///     Initializes the event bus with the supplied options.
    /// </summary>
    public EventBusService(EventBusOptions options)
    {
        _slowListenerThreshold = options.SlowListenerThreshold;
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent eventData)
        where TEvent : IEvent
    {
        PublishAsync(eventData, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();

        var typed = Snapshot(typeof(TEvent));
        var global = typeof(TEvent) == typeof(IEvent) ? null : Snapshot(typeof(IEvent));

        var total = (typed?.Length ?? 0) + (global?.Length ?? 0);

        if (total == 0)
        {
            return;
        }

        if (total == 1)
        {
            var single = typed is { Length: 1 } ? typed[0] : global![0];
            await DispatchSafeAsync(single, eventData, cancellationToken);

            return;
        }

        var tasks = new Task[total];
        var index = 0;

        if (typed is not null)
        {
            for (var i = 0; i < typed.Length; i++)
            {
                tasks[index++] = DispatchSafeAsync(typed[i], eventData, cancellationToken);
            }
        }

        if (global is not null)
        {
            for (var i = 0; i < global.Length; i++)
            {
                tasks[index++] = DispatchSafeAsync(global[i], eventData, cancellationToken);
            }
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public IDisposable RegisterListener<TEvent>(IEventListener<TEvent> listener)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(listener);

        return Add(typeof(TEvent), listener);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        return Add(typeof(TEvent), new DelegateEventListener<TEvent>(handler));
    }

    private Subscription Add(Type eventType, object listener)
    {
        ThrowIfDisposed();

        var bucket = _listeners.GetOrAdd(eventType, static _ => []);

        lock (bucket)
        {
            bucket.Add(listener);
        }

        return new Subscription(bucket, listener);
    }

    private object[]? Snapshot(Type eventType)
    {
        if (!_listeners.TryGetValue(eventType, out var bucket))
        {
            return null;
        }

        lock (bucket)
        {
            return bucket.Count == 0 ? null : bucket.ToArray();
        }
    }

    private async Task DispatchSafeAsync<TEvent>(object listener, TEvent eventData, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            // IEventListener<in TEvent> is contravariant, so a catch-all IEventListener<IEvent>
            // also matches this cast and handles the concrete event.
            if (listener is IEventListener<TEvent> typedListener)
            {
                await typedListener.HandleAsync(eventData, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Event listener {ListenerType} failed for event {EventType}",
                listener.GetType().FullName,
                typeof(TEvent).Name
            );
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            if (elapsed >= _slowListenerThreshold)
            {
                _logger.Warning(
                    "Slow event listener event={EventType} listener={ListenerType} elapsed={ElapsedMs:0.###}ms",
                    typeof(TEvent).Name,
                    listener.GetType().FullName,
                    elapsed.TotalMilliseconds
                );
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EventBusService));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _listeners.Clear();
    }
}
