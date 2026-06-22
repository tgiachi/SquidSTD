using DryIoc;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Core.Json;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;
using SquidStd.Messaging.Services;

namespace SquidStd.Messaging.Extensions;

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

        var serializer = new JsonDataSerializer();
        container.RegisterInstance<IDataSerializer>(serializer, IfAlreadyRegistered.Keep);
        container.RegisterInstance<IDataDeserializer>(serializer, IfAlreadyRegistered.Keep);

        var metrics = new MessagingMetricsProvider();
        container.RegisterInstance<IMessagingMetrics>(metrics);
        container.RegisterInstance<IMetricProvider>(metrics);

        container.Register<IQueueProvider, InMemoryQueueProvider>(Reuse.Singleton);
        container.Register<IMessageQueue, MessageQueue>(Reuse.Singleton);

        return container;
    }

    /// <summary>
    /// Registers the in-memory messaging services from a connection string (scheme must be "memory").
    /// </summary>
    public static IContainer AddInMemoryMessaging(this IContainer container, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(container);

        var cs = MessagingConnectionString.Parse(connectionString);

        if (!string.Equals(cs.Scheme, "memory", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Expected a 'memory://' connection string but got '{cs.Scheme}://'.",
                nameof(connectionString)
            );
        }

        return container.AddInMemoryMessaging(cs.ToMessagingOptions());
    }
}
