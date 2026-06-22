using System.Text;
using SquidStd.Messaging.Extensions;
using SquidStd.Messaging.Services;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Services;

namespace SquidStd.Tests.Messaging;

public class InMemoryQueueProviderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static InMemoryQueueProvider NewProvider(MessagingMetricsProvider? metrics = null, MessagingOptions? options = null)
        => new(options ?? new MessagingOptions(), metrics ?? new MessagingMetricsProvider());

    private static ReadOnlyMemory<byte> Bytes(string s)
        => Encoding.UTF8.GetBytes(s);

    private static string Text(ReadOnlyMemory<byte> b)
        => Encoding.UTF8.GetString(b.Span);

    [Fact]
    public async Task Publish_DeliversToSubscriber()
    {
        await using var provider = NewProvider();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe("q", (payload, _) => { received.TrySetResult(Text(payload)); return Task.CompletedTask; });

        await provider.PublishAsync("q", Bytes("hello"));

        Assert.Equal("hello", await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task MessagesPublishedBeforeSubscribe_AreBuffered()
    {
        await using var provider = NewProvider();
        await provider.PublishAsync("q", Bytes("early"));

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe("q", (payload, _) => { received.TrySetResult(Text(payload)); return Task.CompletedTask; });

        Assert.Equal("early", await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task TwoSubscribers_ReceiveRoundRobin()
    {
        await using var provider = NewProvider();
        var aCount = 0;
        var bCount = 0;
        var done = new CountdownEvent(4);
        provider.Subscribe("q", (_, _) => { Interlocked.Increment(ref aCount); done.Signal(); return Task.CompletedTask; });
        provider.Subscribe("q", (_, _) => { Interlocked.Increment(ref bCount); done.Signal(); return Task.CompletedTask; });

        for (var i = 0; i < 4; i++)
        {
            await provider.PublishAsync("q", Bytes("m"));
        }

        Assert.True(done.Wait(Timeout));
        Assert.Equal(2, aCount);
        Assert.Equal(2, bCount);
    }

    [Fact]
    public async Task FailingThenSucceeding_IsRetriedAndDelivered()
    {
        await using var provider = NewProvider(options: new MessagingOptions { MaxDeliveryAttempts = 3 });
        var attempts = 0;
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe(
            "q",
            (_, _) =>
            {
                if (Interlocked.Increment(ref attempts) < 2)
                {
                    throw new InvalidOperationException("transient");
                }

                delivered.TrySetResult();

                return Task.CompletedTask;
            }
        );

        await provider.PublishAsync("q", Bytes("m"));

        await delivered.Task.WaitAsync(Timeout);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task AlwaysFailing_IsDeadLetteredAfterMaxAttempts()
    {
        await using var provider = NewProvider(options: new MessagingOptions { MaxDeliveryAttempts = 2 });
        var attempts = 0;
        provider.Subscribe("q", (_, _) => { Interlocked.Increment(ref attempts); throw new InvalidOperationException("always"); });

        var deadLettered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.Subscribe("q.dlq", (payload, _) => { deadLettered.TrySetResult(Text(payload)); return Task.CompletedTask; });

        await provider.PublishAsync("q", Bytes("poison"));

        Assert.Equal("poison", await deadLettered.Task.WaitAsync(Timeout));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task DisposedSubscription_StopsReceiving()
    {
        await using var provider = NewProvider();
        var count = 0;
        var subscription = provider.Subscribe("q", (_, _) => { Interlocked.Increment(ref count); return Task.CompletedTask; });
        subscription.Dispose();

        await provider.PublishAsync("q", Bytes("m"));
        await Task.Delay(200);

        Assert.Equal(0, count);
    }
}
