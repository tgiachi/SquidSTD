using System.Threading.Channels;

namespace SquidStd.Messaging.Internal;

/// <summary>
/// Per-queue in-memory state: the buffer channel, the registered handlers, and the round-robin index.
/// </summary>
internal sealed class InMemoryQueue
{
    private readonly Lock _handlerSync = new();
    private readonly List<Func<ReadOnlyMemory<byte>, CancellationToken, Task>> _handlers = [];
    private int _roundRobinIndex;
    private int _depth;

    public Channel<QueuedMessage> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<QueuedMessage>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
    );

    public Task? ConsumerLoop { get; set; }

    /// <summary>Increments the buffered depth and returns the new value.</summary>
    public int IncrementDepth()
        => Interlocked.Increment(ref _depth);

    /// <summary>Decrements the buffered depth and returns the new value.</summary>
    public int DecrementDepth()
        => Interlocked.Decrement(ref _depth);

    public int HandlerCount
    {
        get
        {
            lock (_handlerSync)
            {
                return _handlers.Count;
            }
        }
    }

    public void AddHandler(Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler)
    {
        lock (_handlerSync)
        {
            _handlers.Add(handler);
        }
    }

    public void RemoveHandler(Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler)
    {
        lock (_handlerSync)
        {
            _handlers.Remove(handler);
        }
    }

    /// <summary>Returns the next handler in round-robin order, or null when none are registered.</summary>
    public Func<ReadOnlyMemory<byte>, CancellationToken, Task>? NextHandler()
    {
        lock (_handlerSync)
        {
            if (_handlers.Count == 0)
            {
                return null;
            }

            var handler = _handlers[_roundRobinIndex % _handlers.Count];
            _roundRobinIndex = (_roundRobinIndex + 1) % _handlers.Count;

            return handler;
        }
    }
}
