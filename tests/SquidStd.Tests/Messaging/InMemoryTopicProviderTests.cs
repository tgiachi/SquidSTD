using System.Text;
using SquidStd.Messaging.Services;

namespace SquidStd.Tests.Messaging;

public class InMemoryTopicProviderTests
{
    private static ReadOnlyMemory<byte> Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public async Task Publish_FansOutToAllSubscribers()
    {
        await using var provider = new InMemoryTopicProvider();
        var a = 0;
        var b = 0;
        provider.Subscribe("t", (_, _) => { Interlocked.Increment(ref a); return Task.CompletedTask; });
        provider.Subscribe("t", (_, _) => { Interlocked.Increment(ref b); return Task.CompletedTask; });

        await provider.PublishAsync("t", Bytes("x"));

        Assert.Equal(1, a);
        Assert.Equal(1, b);
    }

    [Fact]
    public async Task Dispose_StopsDelivery()
    {
        await using var provider = new InMemoryTopicProvider();
        var count = 0;
        var subscription = provider.Subscribe("t", (_, _) => { Interlocked.Increment(ref count); return Task.CompletedTask; });

        subscription.Dispose();
        await provider.PublishAsync("t", Bytes("x"));

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Publish_NoSubscribers_IsNoOp()
    {
        await using var provider = new InMemoryTopicProvider();

        await provider.PublishAsync("t", Bytes("x"));
    }

    [Fact]
    public async Task FailingSubscriber_IsIsolated()
    {
        await using var provider = new InMemoryTopicProvider();
        var received = 0;
        provider.Subscribe("t", (_, _) => throw new InvalidOperationException("boom"));
        provider.Subscribe("t", (_, _) => { Interlocked.Increment(ref received); return Task.CompletedTask; });

        await provider.PublishAsync("t", Bytes("x"));

        Assert.Equal(1, received);
    }
}
