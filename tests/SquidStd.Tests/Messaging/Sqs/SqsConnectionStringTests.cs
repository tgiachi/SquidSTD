using DryIoc;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Sqs.Data.Config;
using SquidStd.Messaging.Sqs.Extensions;
using SquidStd.Messaging.Sqs.Services;

namespace SquidStd.Tests.Messaging.Sqs;

public class SqsConnectionStringTests
{
    [Fact]
    public void Parse_MapsRegionCredentialsEndpointAndKnobs()
    {
        var options = SqsMessagingRegistrationExtensions.ParseOptions(
            "sqs://ak:sk@eu-west-1?endpoint=http://localhost:4566&maxMessages=5&visibilityTimeoutSec=15&waitTimeSec=10"
        );

        Assert.Equal("eu-west-1", options.Aws.Region);
        Assert.Equal("ak", options.Aws.AccessKey);
        Assert.Equal("sk", options.Aws.SecretKey);
        Assert.Equal("http://localhost:4566", options.Aws.ServiceUrl);
        Assert.Equal(5, options.MaxNumberOfMessages);
        Assert.Equal(TimeSpan.FromSeconds(15), options.VisibilityTimeout);
        Assert.Equal(10, options.WaitTimeSeconds);
    }

    [Fact]
    public void Parse_WithoutCredentials_LeavesThemNull()
    {
        var options = SqsMessagingRegistrationExtensions.ParseOptions("sqs://us-east-1");

        Assert.Equal("us-east-1", options.Aws.Region);
        Assert.Null(options.Aws.AccessKey);
        Assert.Null(options.Aws.SecretKey);
    }

    [Fact]
    public void WrongScheme_Throws()
        => Assert.Throws<ArgumentException>(() => SqsMessagingRegistrationExtensions.ParseOptions("rabbitmq://x"));

    [Fact]
    public void AddSqsMessaging_RegistersBothProviders()
    {
        using var container = new Container();
        container.AddSqsMessaging(new SqsOptions());

        Assert.IsType<SqsQueueProvider>(container.Resolve<IQueueProvider>());
        Assert.IsType<SqsTopicProvider>(container.Resolve<ITopicProvider>());
        Assert.NotNull(container.Resolve<IMessageQueue>());
        Assert.NotNull(container.Resolve<IMessageTopic>());
    }
}
