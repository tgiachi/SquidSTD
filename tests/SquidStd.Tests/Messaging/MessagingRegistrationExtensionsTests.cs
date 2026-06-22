using DryIoc;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Messaging.Extensions;
using SquidStd.Messaging.Services;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Tests.Messaging;

public class MessagingRegistrationExtensionsTests
{
    [Fact]
    public void AddInMemoryMessaging_RegistersResolvableServices()
    {
        using var container = new Container();

        container.AddInMemoryMessaging();

        Assert.NotNull(container.Resolve<IMessageQueue>());
        Assert.NotNull(container.Resolve<IQueueProvider>());
        Assert.NotNull(container.Resolve<IDataSerializer>());
        Assert.NotNull(container.Resolve<IDataDeserializer>());
        Assert.NotNull(container.Resolve<IMessagingMetrics>());

        Assert.Contains(container.Resolve<IEnumerable<IMetricProvider>>(), p => p.ProviderName == "messaging");
    }

    [Fact]
    public void AddInMemoryMessaging_MetricsAndProviderShareInstance()
    {
        using var container = new Container();

        container.AddInMemoryMessaging();

        Assert.Same(container.Resolve<IMessagingMetrics>(), container.Resolve<IMetricProvider>());
    }
}
