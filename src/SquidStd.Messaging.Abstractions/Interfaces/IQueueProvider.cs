using SquidStd.Abstractions.Interfaces.Services;

namespace SquidStd.Messaging.Abstractions.Interfaces;

/// <summary>
/// Byte-level queue transport owning buffering, round-robin delivery, retry and dead-lettering.
/// </summary>
public interface IQueueProvider : ISquidStdService, IAsyncDisposable
{
    /// <summary>Publishes a raw payload to a named queue.</summary>
    Task PublishAsync(string queueName, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);

    /// <summary>Subscribes a raw handler to a named queue. Dispose to unsubscribe.</summary>
    IDisposable Subscribe(string queueName, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler);
}
