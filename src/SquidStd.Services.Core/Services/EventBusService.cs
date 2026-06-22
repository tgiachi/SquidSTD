using System.Threading.Channels;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Services.Core.Services.Internal;

namespace SquidStd.Services.Core.Services;

/// <summary>
/// Dispatches events to registered listeners through an internal channel.
/// </summary>
public sealed class EventBusService : IEventBus, IDisposable
{
    private readonly Channel<EventDispatch> _dispatches;
    private readonly Task _dispatcher;
    private readonly Lock _listenerSync = new();
    private readonly Dictionary<Type, List<object>> _asyncListeners = [];
    private readonly Dictionary<Type, List<object>> _syncListeners = [];
    private bool _disposed;

    /// <summary>
    /// Initializes the event bus service.
    /// </summary>
    public EventBusService()
    {
        _dispatches = Channel.CreateUnbounded<EventDispatch>(
            new()
            {
                SingleReader = true,
                SingleWriter = false
            }
        );
        _dispatcher = Task.Run(ProcessDispatchesAsync);
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent eventData)
        where TEvent : IEvent
    {
        var listeners = GetListeners<TEvent, ISyncEventListener<TEvent>>(_syncListeners);
        var dispatch = new EventDispatch(
            () =>
            {
                for (var i = 0; i < listeners.Length; i++)
                {
                    listeners[i].Handle(eventData);
                }

                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        Enqueue(dispatch);
        dispatch.Completion.GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();

        var listeners = GetListeners<TEvent, IAsyncEventListener<TEvent>>(_asyncListeners);
        var dispatch = new EventDispatch(
            async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var i = 0; i < listeners.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await listeners[i].HandleAsync(eventData, cancellationToken);
                }
            },
            cancellationToken
        );

        Enqueue(dispatch);
        await dispatch.Completion;
    }

    /// <inheritdoc />
    public void RegisterAsyncListener<TEvent>(IAsyncEventListener<TEvent> listener)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(listener);
        AddListener<TEvent>(_asyncListeners, listener);
    }

    /// <inheritdoc />
    public void RegisterListener<TEvent>(ISyncEventListener<TEvent> listener)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(listener);
        AddListener<TEvent>(_syncListeners, listener);
    }

    private void AddListener<TEvent>(Dictionary<Type, List<object>> listenersByType, object listener)
        where TEvent : IEvent
    {
        lock (_listenerSync)
        {
            ThrowIfDisposed();

            var eventType = typeof(TEvent);

            if (!listenersByType.TryGetValue(eventType, out var listeners))
            {
                listeners = [];
                listenersByType[eventType] = listeners;
            }

            listeners.Add(listener);
        }
    }

    private void Enqueue(EventDispatch dispatch)
    {
        ThrowIfDisposed();

        if (!_dispatches.Writer.TryWrite(dispatch))
        {
            throw new ObjectDisposedException(nameof(EventBusService));
        }
    }

    private TListener[] GetListeners<TEvent, TListener>(Dictionary<Type, List<object>> listenersByType)
        where TEvent : IEvent
        where TListener : class
    {
        lock (_listenerSync)
        {
            ThrowIfDisposed();

            if (!listenersByType.TryGetValue(typeof(TEvent), out var listeners))
            {
                return [];
            }

            return [.. listeners.Cast<TListener>()];
        }
    }

    private async Task ProcessDispatchesAsync()
    {
        await foreach (var dispatch in _dispatches.Reader.ReadAllAsync())
        {
            await dispatch.ExecuteAsync();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EventBusService));
        }
    }

    /// <summary>
    /// Stops the internal dispatcher.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dispatches.Writer.TryComplete();
        _dispatcher.GetAwaiter().GetResult();
    }
}
