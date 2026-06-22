namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Sink for messaging metric events. Implementations must be thread-safe.
/// </summary>
public interface IMessagingMetrics
{
    /// <summary>Records a message published to a queue.</summary>
    void OnPublished(string queueName);

    /// <summary>Records a message delivered successfully.</summary>
    void OnDelivered(string queueName);

    /// <summary>Records a failed delivery attempt.</summary>
    void OnFailed(string queueName);

    /// <summary>Records a retry of a message.</summary>
    void OnRetried(string queueName);

    /// <summary>Records a message moved to the dead-letter queue.</summary>
    void OnDeadLettered(string queueName);

    /// <summary>Sets the current buffered depth of a queue.</summary>
    void SetQueueDepth(string queueName, int depth);

    /// <summary>Sets the current subscriber count of a queue.</summary>
    void SetSubscriberCount(string queueName, int count);
}
