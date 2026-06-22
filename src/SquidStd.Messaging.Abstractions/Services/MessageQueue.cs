using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Abstractions.Services;

/// <summary>
/// Typed facade over an <see cref="IQueueProvider" />: serializes outgoing messages and
/// deserializes incoming payloads before handing them to typed listeners.
/// </summary>
public sealed class MessageQueue : IMessageQueue
{
    private readonly IQueueProvider _provider;
    private readonly IDataSerializer _serializer;
    private readonly IDataDeserializer _deserializer;

    public MessageQueue(IQueueProvider provider, IDataSerializer serializer, IDataDeserializer deserializer)
    {
        _provider = provider;
        _serializer = serializer;
        _deserializer = deserializer;
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
                listener.Handle(_deserializer.Deserialize<TMessage>(payload));

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
            (payload, cancellationToken) => listener.HandleAsync(_deserializer.Deserialize<TMessage>(payload), cancellationToken)
        );
    }
}
