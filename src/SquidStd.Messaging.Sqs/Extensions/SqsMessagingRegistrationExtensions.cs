using System.Globalization;
using DryIoc;
using SquidStd.Aws.Abstractions.Data.Config;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Core.Json;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;
using SquidStd.Messaging.Sqs.Data.Config;
using SquidStd.Messaging.Sqs.Services;

namespace SquidStd.Messaging.Sqs.Extensions;

/// <summary>
///     DryIoc registration helpers for the AWS SQS/SNS messaging provider.
/// </summary>
public static class SqsMessagingRegistrationExtensions
{
    /// <summary>Parses a "sqs://[ak:sk@]region[?params]" connection string into <see cref="SqsOptions" />.</summary>
    public static SqsOptions ParseOptions(string connectionString)
    {
        var cs = MessagingConnectionString.Parse(connectionString);

        if (!string.Equals(cs.Scheme, "sqs", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Expected a 'sqs://' connection string but got '{cs.Scheme}://'.",
                nameof(connectionString)
            );
        }

        return new SqsOptions
        {
            Aws = new AwsConfigEntry
            {
                Region = string.IsNullOrEmpty(cs.Host) ? "us-east-1" : cs.Host,
                AccessKey = cs.UserName,
                SecretKey = cs.Password,
                SessionToken = cs.Parameters.TryGetValue("sessionToken", out var token) ? token : null,
                ServiceUrl = cs.Parameters.TryGetValue("endpoint", out var endpoint) ? endpoint : null
            },
            MaxNumberOfMessages = cs.Parameters.TryGetValue("maxMessages", out var max) &&
                                  int.TryParse(max, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMax)
                ? parsedMax
                : 10,
            VisibilityTimeout = cs.Parameters.TryGetValue("visibilityTimeoutSec", out var vis) &&
                                int.TryParse(vis, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedVis)
                ? TimeSpan.FromSeconds(parsedVis)
                : TimeSpan.FromSeconds(30),
            WaitTimeSeconds = cs.Parameters.TryGetValue("waitTimeSec", out var wait) &&
                              int.TryParse(wait, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedWait)
                ? parsedWait
                : 20
        };
    }

    extension(IContainer container)
    {
        /// <summary>Registers SQS/SNS messaging from an explicit options object.</summary>
        public IContainer AddSqsMessaging(
            SqsOptions options,
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
                r => new SqsQueueProvider(
                    r.Resolve<SqsOptions>(),
                    r.Resolve<MessagingOptions>(),
                    r.Resolve<IMessagingMetrics>()
                ),
                Reuse.Singleton
            );
            container.Register<IMessageQueue, MessageQueue>(Reuse.Singleton);

            container.RegisterDelegate<ITopicProvider>(r => new SqsTopicProvider(r.Resolve<SqsOptions>()), Reuse.Singleton);
            container.Register<IMessageTopic, MessageTopic>(Reuse.Singleton);
            container.Register<ITopicEventBridge, TopicEventBridge>(Reuse.Singleton);

            return container;
        }

        /// <summary>Registers SQS/SNS messaging from a connection string (scheme must be "sqs").</summary>
        public IContainer AddSqsMessaging(string connectionString)
        {
            ArgumentNullException.ThrowIfNull(container);

            var cs = MessagingConnectionString.Parse(connectionString);

            return container.AddSqsMessaging(ParseOptions(connectionString), cs.ToMessagingOptions());
        }
    }
}
