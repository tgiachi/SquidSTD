using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Json;
using SquidStd.Messaging.Abstractions.Data.Events;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;
using SquidStd.Messaging.Services;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Messaging;

public class TopicEventBridgeTests
{
    private sealed class Beat
    {
        public string Worker { get; set; } = "";
    }

    private sealed class CapturingListener : IAsyncEventListener<TopicMessageEvent>
    {
        public TaskCompletionSource<TopicMessageEvent> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task HandleAsync(TopicMessageEvent eventData, CancellationToken cancellationToken)
        {
            Received.TrySetResult(eventData);

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Bridge_RepublishesTopicMessageOnEventBus()
    {
        await using var provider = new InMemoryTopicProvider();
        var serializer = new JsonDataSerializer();
        IMessageTopic topic = new MessageTopic(provider, serializer, serializer);
        var eventBus = new EventBusService();
        ITopicEventBridge bridge = new TopicEventBridge(topic, eventBus);

        var listener = new CapturingListener();
        eventBus.RegisterAsyncListener(listener);
        using var subscription = bridge.Bridge<Beat>("workers.heartbeat");

        await topic.PublishAsync("workers.heartbeat", new Beat { Worker = "w1" });

        var evt = await listener.Received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("workers.heartbeat", evt.Topic);
        Assert.Equal("w1", Assert.IsType<Beat>(evt.Data).Worker);
    }
}
