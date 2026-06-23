namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Typed facade for publishing to and subscribing to topics (fan-out).
/// </summary>
public interface IMessageTopic
{
    /// <summary>Publishes a typed message to a topic.</summary>
    Task PublishAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default);

    /// <summary>Subscribes a typed handler to a topic. Dispose to unsubscribe.</summary>
    IDisposable Subscribe<TMessage>(string topic, Func<TMessage, CancellationToken, Task> handler);
}
