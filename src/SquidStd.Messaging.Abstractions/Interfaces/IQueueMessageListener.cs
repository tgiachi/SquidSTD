namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Handles a queue message synchronously.
/// </summary>
/// <typeparam name="TMessage">The message payload type.</typeparam>
public interface IQueueMessageListener<in TMessage>
{
    /// <summary>Handles a delivered message.</summary>
    void Handle(TMessage message);
}
