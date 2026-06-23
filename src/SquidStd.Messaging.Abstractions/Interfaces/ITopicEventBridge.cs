namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Bridges a topic into the in-process event bus: each message of type <c>T</c> on the topic is republished
/// as a <c>TopicMessageEvent</c>.
/// </summary>
public interface ITopicEventBridge
{
    /// <summary>Starts bridging the given topic; dispose the result to stop.</summary>
    IDisposable Bridge<T>(string topic);
}
