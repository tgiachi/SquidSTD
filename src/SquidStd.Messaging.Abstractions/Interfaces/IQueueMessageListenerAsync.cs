namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Handles a queue message asynchronously.
/// </summary>
/// <typeparam name="TMessage">The message payload type.</typeparam>
public interface IQueueMessageListenerAsync<in TMessage>
{
    /// <summary>Handles a delivered message.</summary>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
