using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Abstractions;

/// <summary>
/// Typed facade over an <see cref="IQueueProvider" />: serializes outgoing messages and
/// deserializes incoming payloads before handing them to typed listeners.
/// </summary>
public sealed class MessageQueue : IMessageQueue
{
    private readonly IQueueProvider _provider;
    private readonly IMessageSerializer _serializer;

    public MessageQueue(IQueueProvider provider, IMessageSerializer serializer)
    {
        _provider = provider;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(string queueName, TMessage message, CancellationToken cancellationToken = default)
        => _provider.PublishAsync(queueName, _serializer.Serialize(message), cancellationToken);

    /// <inheritdoc />
    public IDisposable Subscribe<TMessage>(string queueName, IQueueMessageListener<TMessage> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        return _provider.Subscribe(
            queueName,
            (payload, _) =>
            {
                listener.Handle(_serializer.Deserialize<TMessage>(payload));

                return Task.CompletedTask;
            }
        );
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TMessage>(string queueName, IQueueMessageListenerAsync<TMessage> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        return _provider.Subscribe(
            queueName,
            (payload, cancellationToken) => listener.HandleAsync(_serializer.Deserialize<TMessage>(payload), cancellationToken)
        );
    }
}
