using System.Collections.Concurrent;
using Serilog;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Services;

/// <summary>
/// In-memory <see cref="ITopicProvider" />: fan-out delivery to all current subscribers of a topic.
/// Transient and at-most-once; exceptions in one subscriber are isolated.
/// </summary>
public sealed class InMemoryTopicProvider : ITopicProvider
{
    private readonly ILogger _logger = Log.ForContext<InMemoryTopicProvider>();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Func<ReadOnlyMemory<byte>, CancellationToken, Task>>> _topics = new(StringComparer.Ordinal);
    private int _disposed;

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
        => DisposeAsync();

    /// <inheritdoc />
    public async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        if (!_topics.TryGetValue(topic, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Values)
        {
            try
            {
                await handler(payload, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Topic '{Topic}' subscriber failed", topic);
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(string topic, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _topics.GetOrAdd(topic, static _ => new());
        var id = Guid.NewGuid();
        handlers[id] = handler;

        return new Subscription(handlers, id);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _topics.Clear();

        return ValueTask.CompletedTask;
    }

    private sealed class Subscription : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, Func<ReadOnlyMemory<byte>, CancellationToken, Task>> _handlers;
        private readonly Guid _id;
        private int _disposed;

        public Subscription(ConcurrentDictionary<Guid, Func<ReadOnlyMemory<byte>, CancellationToken, Task>> handlers, Guid id)
        {
            _handlers = handlers;
            _id = id;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _handlers.TryRemove(_id, out _);
        }
    }
}
