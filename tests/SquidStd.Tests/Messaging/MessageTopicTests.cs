using SquidStd.Core.Json;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;
using SquidStd.Messaging.Services;

namespace SquidStd.Tests.Messaging;

public class MessageTopicTests
{
    private sealed class Ping
    {
        public string Source { get; set; } = "";
    }

    [Fact]
    public async Task PublishSubscribe_RoundTripsTypedMessage()
    {
        await using var provider = new InMemoryTopicProvider();
        var serializer = new JsonDataSerializer();
        IMessageTopic topic = new MessageTopic(provider, serializer, serializer);
        var received = new TaskCompletionSource<Ping>(TaskCreationOptions.RunContinuationsAsynchronously);
        topic.Subscribe<Ping>("pings", (ping, _) => { received.TrySetResult(ping); return Task.CompletedTask; });

        await topic.PublishAsync("pings", new Ping { Source = "w1" });

        var got = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("w1", got.Source);
    }
}
