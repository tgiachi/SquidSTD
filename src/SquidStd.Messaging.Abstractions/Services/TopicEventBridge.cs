using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Data.Events;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Abstractions.Services;

/// <summary>
/// One-way Topic → EventBus bridge. Subscribes a topic and republishes each message as a
/// <see cref="TopicMessageEvent" /> on the <see cref="IEventBus" />.
/// </summary>
public sealed class TopicEventBridge : ITopicEventBridge
{
    private readonly IMessageTopic _topic;
    private readonly IEventBus _eventBus;

    public TopicEventBridge(IMessageTopic topic, IEventBus eventBus)
    {
        _topic = topic;
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public IDisposable Bridge<T>(string topic)
        => _topic.Subscribe<T>(topic, (data, cancellationToken) => _eventBus.PublishAsync(new TopicMessageEvent(topic, data!), cancellationToken));
}
