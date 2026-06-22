using SquidStd.Messaging.Abstractions.Services;

namespace SquidStd.Tests.Messaging;

public class MessagingMetricsProviderTests
{
    [Fact]
    public void ProviderName_IsMessaging()
        => Assert.Equal("messaging", new MessagingMetricsProvider().ProviderName);

    [Fact]
    public async Task CollectAsync_ReportsCountersAndGaugesPerQueue()
    {
        var metrics = new MessagingMetricsProvider();

        metrics.OnPublished("orders");
        metrics.OnPublished("orders");
        metrics.OnDelivered("orders");
        metrics.OnFailed("orders");
        metrics.OnRetried("orders");
        metrics.OnDeadLettered("orders");
        metrics.SetQueueDepth("orders", 5);
        metrics.SetSubscriberCount("orders", 2);

        var samples = await metrics.CollectAsync();

        double Value(string name) => samples.Single(
            s => s.Name == name && s.Tags != null && s.Tags["queue"] == "orders").Value;

        Assert.Equal(2, Value("published"));
        Assert.Equal(1, Value("delivered"));
        Assert.Equal(1, Value("failed"));
        Assert.Equal(1, Value("retried"));
        Assert.Equal(1, Value("dead_lettered"));
        Assert.Equal(5, Value("queue_depth"));
        Assert.Equal(2, Value("subscribers"));
    }
}
