using SquidStd.Messaging.Abstractions;
using SquidStd.Messaging.Abstractions.Data.Config;

namespace SquidStd.Tests.Messaging;

public class MessagingConnectionStringTests
{
    [Fact]
    public void Parse_Memory_ReadsScheme()
    {
        var cs = MessagingConnectionString.Parse("memory://localhost");

        Assert.Equal("memory", cs.Scheme);
        Assert.Equal("localhost", cs.Host);
        Assert.Null(cs.Port);
    }

    [Fact]
    public void Parse_RabbitMq_ReadsCredentialsHostPortVhost()
    {
        var cs = MessagingConnectionString.Parse("rabbitmq://user:pass@broker:5672/myvhost");

        Assert.Equal("rabbitmq", cs.Scheme);
        Assert.Equal("user", cs.UserName);
        Assert.Equal("pass", cs.Password);
        Assert.Equal("broker", cs.Host);
        Assert.Equal(5672, cs.Port);
        Assert.Equal("myvhost", cs.VirtualHost);
    }

    [Fact]
    public void Parse_ReadsQueryParameters()
    {
        var cs = MessagingConnectionString.Parse("rabbitmq://broker/?prefetch=20&maxDeliveryAttempts=5");

        Assert.Equal("20", cs.Parameters["prefetch"]);
        Assert.Equal("5", cs.Parameters["maxDeliveryAttempts"]);
    }

    [Fact]
    public void ToMessagingOptions_ReadsParameters()
    {
        var cs = MessagingConnectionString.Parse("rabbitmq://broker/?maxDeliveryAttempts=5&deadLetterSuffix=.dead&retryDelayMs=250");

        var options = cs.ToMessagingOptions();

        Assert.Equal(5, options.MaxDeliveryAttempts);
        Assert.Equal(".dead", options.DeadLetterQueueSuffix);
        Assert.Equal(TimeSpan.FromMilliseconds(250), options.RetryDelay);
    }

    [Fact]
    public void Parse_NullOrWhitespace_Throws()
        => Assert.Throws<ArgumentException>(() => MessagingConnectionString.Parse("  "));
}
