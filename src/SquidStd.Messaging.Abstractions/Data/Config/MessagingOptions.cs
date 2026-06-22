namespace SquidStd.Messaging.Abstractions.Data.Config;

/// <summary>
/// Configuration for the messaging system.
/// </summary>
public sealed class MessagingOptions
{
    /// <summary>Maximum delivery attempts before dead-lettering. Default 3.</summary>
    public int MaxDeliveryAttempts { get; init; } = 3;

    /// <summary>Suffix appended to a queue name to form its dead-letter queue. Default ".dlq".</summary>
    public string DeadLetterQueueSuffix { get; init; } = ".dlq";

    /// <summary>Delay applied before re-enqueueing a failed message. Default zero.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.Zero;
}
