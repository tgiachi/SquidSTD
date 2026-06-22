using System.Collections.Concurrent;
using Serilog;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;
using SquidStd.Messaging.Internal;

namespace SquidStd.Messaging.Services;

/// <summary>
/// In-memory <see cref="IQueueProvider" />: one buffered channel + consumer loop per named queue,
/// round-robin delivery, retry and dead-lettering.
/// </summary>
public sealed class InMemoryQueueProvider : IQueueProvider
{
    private readonly ILogger _logger = Log.ForContext<InMemoryQueueProvider>();
    private readonly ConcurrentDictionary<string, InMemoryQueue> _queues = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly MessagingOptions _options;
    private readonly IMessagingMetrics _metrics;
    private readonly TimeProvider _timeProvider;
    private int _disposed;

    public InMemoryQueueProvider(MessagingOptions options, IMessagingMetrics? metrics = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _metrics = metrics ?? NoOpMessagingMetrics.Instance;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
        => DisposeAsync();

    /// <inheritdoc />
    public Task PublishAsync(string queueName, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        cancellationToken.ThrowIfCancellationRequested();

        Enqueue(queueName, new(payload, 0));
        _metrics.OnPublished(queueName);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IDisposable Subscribe(string queueName, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(handler);

        var queue = GetOrCreate(queueName);
        queue.AddHandler(handler);
        _metrics.SetSubscriberCount(queueName, queue.HandlerCount);

        return new Subscription(this, queueName, queue, handler);
    }

    private InMemoryQueue GetOrCreate(string queueName)
        => _queues.GetOrAdd(
            queueName,
            name =>
            {
                var queue = new InMemoryQueue();
                queue.ConsumerLoop = Task.Run(() => ConsumeLoopAsync(name, queue, _shutdownCts.Token), CancellationToken.None);

                return queue;
            }
        );

    private void Enqueue(string queueName, QueuedMessage message)
        => Write(GetOrCreate(queueName), queueName, message);

    private void Write(InMemoryQueue queue, string queueName, QueuedMessage message)
    {
        queue.Channel.Writer.TryWrite(message);
        _metrics.SetQueueDepth(queueName, queue.IncrementDepth());
    }

    private async Task ConsumeLoopAsync(string queueName, InMemoryQueue queue, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in queue.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                _metrics.SetQueueDepth(queueName, queue.DecrementDepth());

                var handler = await WaitForHandlerAsync(queue, cancellationToken);

                if (handler is null)
                {
                    return; // shutting down
                }

                try
                {
                    await handler(message.Payload, cancellationToken);
                    _metrics.OnDelivered(queueName);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    HandleFailure(queueName, queue, message, ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "In-memory queue consumer loop failed for {QueueName}", queueName);
        }
    }

    private async Task<Func<ReadOnlyMemory<byte>, CancellationToken, Task>?> WaitForHandlerAsync(
        InMemoryQueue queue,
        CancellationToken cancellationToken
    )
    {
        // Messages can arrive before any subscriber; wait until a handler exists.
        while (!cancellationToken.IsCancellationRequested)
        {
            var handler = queue.NextHandler();

            if (handler is not null)
            {
                return handler;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), _timeProvider, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    private void HandleFailure(string queueName, InMemoryQueue queue, QueuedMessage message, Exception exception)
    {
        _metrics.OnFailed(queueName);
        var nextAttempt = message.Attempt + 1;

        if (nextAttempt < _options.MaxDeliveryAttempts)
        {
            _metrics.OnRetried(queueName);
            _ = RequeueAsync(queue, queueName, message with { Attempt = nextAttempt });

            return;
        }

        _logger.Warning(exception, "Message dead-lettered on queue {QueueName} after {Attempts} attempts", queueName, nextAttempt);
        _metrics.OnDeadLettered(queueName);
        Enqueue(queueName + _options.DeadLetterQueueSuffix, new(message.Payload, 0));
    }

    private async Task RequeueAsync(InMemoryQueue queue, string queueName, QueuedMessage message)
    {
        if (_options.RetryDelay > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(_options.RetryDelay, _timeProvider, _shutdownCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        Write(queue, queueName, message);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _shutdownCts.CancelAsync();

        foreach (var queue in _queues.Values)
        {
            queue.Channel.Writer.TryComplete();
        }

        foreach (var queue in _queues.Values)
        {
            if (queue.ConsumerLoop is not null)
            {
                try
                {
                    await queue.ConsumerLoop;
                }
                catch
                {
                    // Loop failures are already logged.
                }
            }
        }

        _shutdownCts.Dispose();
    }

    private sealed class Subscription : IDisposable
    {
        private readonly InMemoryQueueProvider _provider;
        private readonly string _queueName;
        private readonly InMemoryQueue _queue;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _handler;
        private int _disposed;

        public Subscription(
            InMemoryQueueProvider provider,
            string queueName,
            InMemoryQueue queue,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler
        )
        {
            _provider = provider;
            _queueName = queueName;
            _queue = queue;
            _handler = handler;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _queue.RemoveHandler(_handler);
            _provider._metrics.SetSubscriberCount(_queueName, _queue.HandlerCount);
        }
    }
}
