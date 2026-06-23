using DryIoc;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Core.Json;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;

using SquidStd.Messaging.RabbitMq.Data.Config;
using SquidStd.Messaging.RabbitMq.Services;

namespace SquidStd.Messaging.RabbitMq.Extensions;

/// <summary>
/// DryIoc registration helpers for the RabbitMQ messaging provider.
/// </summary>
public static class RabbitMqMessagingRegistrationExtensions
{
    /// <summary>Registers RabbitMQ messaging from an explicit options object.</summary>
    public static IContainer AddRabbitMqMessaging(
        this IContainer container,
        RabbitMqOptions options,
        MessagingOptions? messagingOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(options);

        var resolvedMessaging = messagingOptions ?? new MessagingOptions();

        container.RegisterInstance(resolvedMessaging);
        container.RegisterInstance(options);

        var serializer = new JsonDataSerializer();
        container.RegisterInstance<IDataSerializer>(serializer, IfAlreadyRegistered.Keep);
        container.RegisterInstance<IDataDeserializer>(serializer, IfAlreadyRegistered.Keep);

        var metrics = new MessagingMetricsProvider();
        container.RegisterInstance<IMessagingMetrics>(metrics);
        container.RegisterInstance<IMetricProvider>(metrics);

        container.RegisterDelegate<IQueueProvider>(
            r => new RabbitMqQueueProvider(
                r.Resolve<RabbitMqOptions>(),
                r.Resolve<MessagingOptions>(),
                r.Resolve<IMessagingMetrics>()
            ),
            Reuse.Singleton
        );
        container.Register<IMessageQueue, MessageQueue>(Reuse.Singleton);

        container.Register<ITopicProvider, RabbitMqTopicProvider>(Reuse.Singleton);
        container.Register<IMessageTopic, MessageTopic>(Reuse.Singleton);
        container.Register<ITopicEventBridge, TopicEventBridge>(Reuse.Singleton);

        return container;
    }

    /// <summary>Registers RabbitMQ messaging from a connection string (scheme must be "rabbitmq").</summary>
    public static IContainer AddRabbitMqMessaging(this IContainer container, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(container);

        var cs = MessagingConnectionString.Parse(connectionString);

        if (!string.Equals(cs.Scheme, "rabbitmq", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Expected a 'rabbitmq://' connection string but got '{cs.Scheme}://'.",
                nameof(connectionString)
            );
        }

        var options = new RabbitMqOptions
        {
            HostName = string.IsNullOrEmpty(cs.Host) ? "localhost" : cs.Host,
            Port = cs.Port ?? 5672,
            VirtualHost = cs.VirtualHost,
            UserName = cs.UserName ?? "guest",
            Password = cs.Password ?? "guest",
            PrefetchCount = cs.Parameters.TryGetValue("prefetch", out var prefetch) && ushort.TryParse(prefetch, out var parsed)
                                ? parsed
                                : (ushort)10
        };

        return container.AddRabbitMqMessaging(options, cs.ToMessagingOptions());
    }
}
