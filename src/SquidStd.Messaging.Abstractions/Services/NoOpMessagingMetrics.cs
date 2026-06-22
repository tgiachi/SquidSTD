using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Abstractions.Services;

/// <summary>
/// Metrics sink that ignores all events. Used when no metrics are configured.
/// </summary>
public sealed class NoOpMessagingMetrics : IMessagingMetrics
{
    /// <summary>Shared instance.</summary>
    public static NoOpMessagingMetrics Instance { get; } = new();

    public void OnPublished(string queueName)
    {
    }

    public void OnDelivered(string queueName)
    {
    }

    public void OnFailed(string queueName)
    {
    }

    public void OnRetried(string queueName)
    {
    }

    public void OnDeadLettered(string queueName)
    {
    }

    public void SetQueueDepth(string queueName, int depth)
    {
    }

    public void SetSubscriberCount(string queueName, int count)
    {
    }
}
