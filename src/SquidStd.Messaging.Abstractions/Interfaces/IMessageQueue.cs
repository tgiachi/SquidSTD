namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Typed facade for publishing to and subscribing to named queues.
/// </summary>
public interface IMessageQueue
{
    /// <summary>Publishes a message to a named queue.</summary>
    Task PublishAsync<TMessage>(string queueName, TMessage message, CancellationToken cancellationToken = default);

    /// <summary>Subscribes a synchronous listener to a named queue. Dispose to unsubscribe.</summary>
    IDisposable Subscribe<TMessage>(string queueName, IQueueMessageListener<TMessage> listener);

    /// <summary>Subscribes an asynchronous listener to a named queue. Dispose to unsubscribe.</summary>
    IDisposable Subscribe<TMessage>(string queueName, IQueueMessageListenerAsync<TMessage> listener);
}
