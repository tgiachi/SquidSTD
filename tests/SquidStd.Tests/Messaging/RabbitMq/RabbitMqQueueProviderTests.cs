using System.Text;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.RabbitMq.Data.Config;
using SquidStd.Messaging.RabbitMq.Services;

namespace SquidStd.Tests.Messaging.RabbitMq;

[Collection(RabbitMqCollection.Name)]
public class RabbitMqQueueProviderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly RabbitMqContainerFixture _fixture;

    public RabbitMqQueueProviderTests(RabbitMqContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private RabbitMqQueueProvider NewProvider(MessagingOptions? options = null)
        => new(new RabbitMqOptions { Uri = new Uri(_fixture.AmqpUri) }, options ?? new MessagingOptions());

    private static ReadOnlyMemory<byte> Bytes(string s) => Encoding.UTF8.GetBytes(s);
    private static string Text(ReadOnlyMemory<byte> b) => Encoding.UTF8.GetString(b.Span);
    private static string Queue() => "q-" + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task Publish_DeliversToSubscriber()
    {
        await using var provider = NewProvider();
        await provider.StartAsync();
        var queue = Queue();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe(queue, (payload, _) => { received.TrySetResult(Text(payload)); return Task.CompletedTask; });

        await provider.PublishAsync(queue, Bytes("hello"));

        Assert.Equal("hello", await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task TwoSubscribers_ShareTheLoad()
    {
        await using var provider = NewProvider();
        await provider.StartAsync();
        var queue = Queue();
        const int total = 10;
        var a = 0;
        var b = 0;
        using var done = new CountdownEvent(total);
        provider.Subscribe(queue, (_, _) => { Interlocked.Increment(ref a); done.Signal(); return Task.CompletedTask; });
        provider.Subscribe(queue, (_, _) => { Interlocked.Increment(ref b); done.Signal(); return Task.CompletedTask; });

        for (var i = 0; i < total; i++)
        {
            await provider.PublishAsync(queue, Bytes("m"));
        }

        Assert.True(done.Wait(Timeout));
        Assert.Equal(total, a + b);
        Assert.True(a > 0);
        Assert.True(b > 0);
    }

    [Fact]
    public async Task AlwaysFailing_IsDeadLettered()
    {
        await using var provider = NewProvider(new MessagingOptions { MaxDeliveryAttempts = 2 });
        await provider.StartAsync();
        var queue = Queue();
        provider.Subscribe(queue, (_, _) => throw new InvalidOperationException("always"));

        var deadLettered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe(queue + ".dlq", (payload, _) => { deadLettered.TrySetResult(Text(payload)); return Task.CompletedTask; });

        await provider.PublishAsync(queue, Bytes("poison"));

        Assert.Equal("poison", await deadLettered.Task.WaitAsync(Timeout));
    }
}
