using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Services;

namespace SquidStd.Tests.Messaging;

public class MessagingOptionsTests
{
    [Fact]
    public void Defaults_AreApplied()
    {
        var options = new MessagingOptions();

        Assert.Equal(3, options.MaxDeliveryAttempts);
        Assert.Equal(".dlq", options.DeadLetterQueueSuffix);
        Assert.Equal(TimeSpan.Zero, options.RetryDelay);
    }
}
