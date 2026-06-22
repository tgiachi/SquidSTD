using DryIoc;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Messaging.Abstractions;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging;

/// <summary>
/// DryIoc registration helpers for the in-memory messaging system.
/// </summary>
public static class MessagingRegistrationExtensions
{
    /// <summary>
    /// Registers the in-memory messaging services (facade, provider, serializer, metrics).
    /// </summary>
    /// <param name="container">The container to register into.</param>
    /// <param name="options">Optional messaging options; defaults are used when null.</param>
    /// <returns>The container for chaining.</returns>
    public static IContainer AddInMemoryMessaging(this IContainer container, MessagingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.RegisterInstance(options ?? new MessagingOptions());
        container.Register<IMessageSerializer, JsonMessageSerializer>(Reuse.Singleton);

        var metrics = new MessagingMetricsProvider();
        container.RegisterInstance<IMessagingMetrics>(metrics);
        container.RegisterInstance<IMetricProvider>(metrics);

        container.Register<IQueueProvider, InMemoryQueueProvider>(Reuse.Singleton);
        container.Register<IMessageQueue, MessageQueue>(Reuse.Singleton);

        return container;
    }
}
