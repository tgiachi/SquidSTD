using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Abstractions.Services;

/// <summary>
/// Typed facade over an <see cref="ITopicProvider" />: serializes outgoing messages and deserializes
/// incoming payloads.
/// </summary>
public sealed class MessageTopic : IMessageTopic
{
    private readonly ITopicProvider _provider;
    private readonly IDataSerializer _serializer;
    private readonly IDataDeserializer _deserializer;

    public MessageTopic(ITopicProvider provider, IDataSerializer serializer, IDataDeserializer deserializer)
    {
        _provider = provider;
        _serializer = serializer;
        _deserializer = deserializer;
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default)
        => _provider.PublishAsync(topic, _serializer.Serialize(message), cancellationToken);

    /// <inheritdoc />
    public IDisposable Subscribe<TMessage>(string topic, Func<TMessage, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return _provider.Subscribe(topic, (payload, cancellationToken) => handler(_deserializer.Deserialize<TMessage>(payload), cancellationToken));
    }
}
