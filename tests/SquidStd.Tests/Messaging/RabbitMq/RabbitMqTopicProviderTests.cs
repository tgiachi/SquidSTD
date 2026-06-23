using System.Text;
using SquidStd.Messaging.RabbitMq.Data.Config;
using SquidStd.Messaging.RabbitMq.Services;

namespace SquidStd.Tests.Messaging.RabbitMq;

[Collection(RabbitMqCollection.Name)]
public class RabbitMqTopicProviderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly RabbitMqContainerFixture _fixture;

    public RabbitMqTopicProviderTests(RabbitMqContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private RabbitMqTopicProvider NewProvider()
        => new(new RabbitMqOptions { Uri = new Uri(_fixture.AmqpUri) });

    private static ReadOnlyMemory<byte> Bytes(string s) => Encoding.UTF8.GetBytes(s);
    private static string Topic() => "topic-" + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task Publish_FansOutToAllSubscribers()
    {
        await using var provider = NewProvider();
        await provider.StartAsync();
        var topic = Topic();
        var a = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var b = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe(topic, (payload, _) => { a.TrySetResult(Encoding.UTF8.GetString(payload.Span)); return Task.CompletedTask; });
        provider.Subscribe(topic, (payload, _) => { b.TrySetResult(Encoding.UTF8.GetString(payload.Span)); return Task.CompletedTask; });

        // Allow the broker to register the bindings before publishing.
        await Task.Delay(300);
        await provider.PublishAsync(topic, Bytes("hello"));

        Assert.Equal("hello", await a.Task.WaitAsync(Timeout));
        Assert.Equal("hello", await b.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task Publish_BeforeAnySubscriber_IsNotReceived()
    {
        await using var provider = NewProvider();
        await provider.StartAsync();
        var topic = Topic();

        await provider.PublishAsync(topic, Bytes("lost"));

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe(topic, (payload, _) => { received.TrySetResult(Encoding.UTF8.GetString(payload.Span)); return Task.CompletedTask; });
        await Task.Delay(300);

        await Assert.ThrowsAsync<TimeoutException>(async () => await received.Task.WaitAsync(TimeSpan.FromSeconds(1)));
    }
}
