using SquidStd.Abstractions.Interfaces.Services;

namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Byte-level publish/subscribe transport: every current subscriber of a topic receives every message
/// (transient, at-most-once fan-out).
/// </summary>
public interface ITopicProvider : ISquidStdService, IAsyncDisposable
{
    /// <summary>Publishes a raw payload to a topic (fan-out to all current subscribers).</summary>
    Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);

    /// <summary>Subscribes a raw handler to a topic. Dispose to unsubscribe.</summary>
    IDisposable Subscribe(string topic, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler);
}
