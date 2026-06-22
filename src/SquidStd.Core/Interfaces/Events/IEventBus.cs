namespace SquidStd.Core.Interfaces.Events;

/// <summary>
/// Dispatches synchronous and asynchronous events to registered listeners.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Dispatches an event to synchronous listeners and waits until every listener has completed.
    /// </summary>
    /// <param name="eventData">The event payload.</param>
    /// <typeparam name="TEvent">The event type.</typeparam>
    void Publish<TEvent>(TEvent eventData) where TEvent : IEvent;

    /// <summary>
    /// Dispatches an event to asynchronous listeners and waits until every listener has completed.
    /// </summary>
    /// <param name="eventData">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <returns>A task that completes after all asynchronous listeners finish.</returns>
    Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken) where TEvent : IEvent;

    /// <summary>
    /// Registers an asynchronous listener for the specified event type.
    /// </summary>
    /// <param name="listener">Listener that handles published events asynchronously.</param>
    /// <typeparam name="TEvent">The event type.</typeparam>
    void RegisterAsyncListener<TEvent>(IAsyncEventListener<TEvent> listener) where TEvent : IEvent;

    /// <summary>
    /// Registers a synchronous listener for the specified event type.
    /// </summary>
    /// <param name="listener">Listener that handles published events.</param>
    /// <typeparam name="TEvent">The event type.</typeparam>
    void RegisterListener<TEvent>(ISyncEventListener<TEvent> listener) where TEvent : IEvent;
}
