using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;
using SquidStd.Messaging.RabbitMq.Data.Config;

namespace SquidStd.Messaging.RabbitMq.Services;

/// <summary>
///     RabbitMQ <see cref="IQueueProvider" />: named queues map to quorum queues with a delivery limit
///     and a dead-letter exchange; round-robin is the broker's native competing-consumers behaviour.
/// </summary>
public sealed class RabbitMqQueueProvider : IQueueProvider
{
    private readonly HashSet<string> _declared = new(StringComparer.Ordinal);
    private readonly ILogger _logger = Log.ForContext<RabbitMqQueueProvider>();
    private readonly MessagingOptions _messagingOptions;
    private readonly IMessagingMetrics _metrics;
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private readonly ConcurrentDictionary<string, int> _subscriberCounts = new(StringComparer.Ordinal);
    private readonly Lock _topologySync = new();
    private IConnection? _connection;
    private int _disposed;
    private IChannel? _publishChannel;

    public RabbitMqQueueProvider(
        RabbitMqOptions options,
        MessagingOptions messagingOptions,
        IMessagingMetrics? metrics = null
    )
    {
        _options = options;
        _messagingOptions = messagingOptions;
        _metrics = metrics ?? NoOpMessagingMetrics.Instance;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (_publishChannel is not null)
        {
            await _publishChannel.CloseAsync();
            await _publishChannel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _publishLock.Dispose();
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        string queueName,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        var channel = _publishChannel ?? throw new InvalidOperationException("Provider not started.");

        await EnsureTopologyAsync(channel, queueName, cancellationToken);

        var properties = new BasicProperties { Persistent = true };

        await _publishLock.WaitAsync(cancellationToken);

        try
        {
            await channel.BasicPublishAsync(
                string.Empty,
                queueName,
                false,
                properties,
                payload,
                cancellationToken
            );
        }
        finally
        {
            _publishLock.Release();
        }

        _metrics.OnPublished(queueName);
    }

    /// <inheritdoc />
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory { AutomaticRecoveryEnabled = true };

        if (_options.Uri is not null)
        {
            factory.Uri = _options.Uri;
        }
        else
        {
            factory.HostName = _options.HostName;
            factory.Port = _options.Port;
            factory.VirtualHost = _options.VirtualHost;
            factory.UserName = _options.UserName;
            factory.Password = _options.Password;
        }

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _publishChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return DisposeAsync();
    }

    /// <inheritdoc />
    public IDisposable Subscribe(string queueName, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(handler);

        var connection = _connection ?? throw new InvalidOperationException("Provider not started.");
        var subscription = new Subscription(this, connection, queueName, handler);
        subscription.Start();
        _metrics.SetSubscriberCount(queueName, _subscriberCounts.AddOrUpdate(queueName, 1, static (_, count) => count + 1));

        return subscription;
    }

    private async Task EnsureTopologyAsync(IChannel channel, string queueName, CancellationToken cancellationToken)
    {
        lock (_topologySync)
        {
            if (!_declared.Add(queueName))
            {
                return;
            }
        }

        try
        {
            // Dead-letter queues are terminal: declare them plainly, no DLX.
            if (queueName.EndsWith(_messagingOptions.DeadLetterQueueSuffix, StringComparison.Ordinal))
            {
                await channel.QueueDeclareAsync(
                    queueName,
                    true,
                    false,
                    false,
                    new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
                    cancellationToken: cancellationToken
                );

                return;
            }

            var deadLetterQueue = queueName + _messagingOptions.DeadLetterQueueSuffix;

            await channel.QueueDeclareAsync(
                deadLetterQueue,
                true,
                false,
                false,
                new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
                cancellationToken: cancellationToken
            );

            await channel.QueueDeclareAsync(
                queueName,
                true,
                false,
                false,
                new Dictionary<string, object?>
                {
                    ["x-queue-type"] = "quorum",
                    ["x-delivery-limit"] = _messagingOptions.MaxDeliveryAttempts,
                    ["x-dead-letter-exchange"] = string.Empty,
                    ["x-dead-letter-routing-key"] = deadLetterQueue
                },
                cancellationToken: cancellationToken
            );
        }
        catch
        {
            // A declare failed, so the topology is not actually established: undo the optimistic
            // mark so a later call retries instead of assuming the queues exist.
            lock (_topologySync)
            {
                _declared.Remove(queueName);
            }

            throw;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly IConnection _connection;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _handler;
        private readonly RabbitMqQueueProvider _provider;
        private readonly string _queueName;
        private IChannel? _channel;
        private string? _consumerTag;
        private int _disposed;

        public Subscription(
            RabbitMqQueueProvider provider,
            IConnection connection,
            string queueName,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler
        )
        {
            _provider = provider;
            _connection = connection;
            _queueName = queueName;
            _handler = handler;
        }

        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
        {
            var channel = _channel!;

            try
            {
                await _handler(args.Body, CancellationToken.None);
                await channel.BasicAckAsync(args.DeliveryTag, false);
                _provider._metrics.OnDelivered(_queueName);
            }
            catch (Exception ex)
            {
                _provider._logger.Warning(ex, "RabbitMq handler failed for {QueueName}", _queueName);
                _provider._metrics.OnFailed(_queueName);
                await channel.BasicNackAsync(args.DeliveryTag, false, true);
            }
        }

        private async Task StartAsync()
        {
            _channel = await _connection.CreateChannelAsync();
            await _provider.EnsureTopologyAsync(_channel, _queueName, CancellationToken.None);
            await _channel.BasicQosAsync(0, _provider._options.PrefetchCount, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnReceivedAsync;

            _consumerTag = await _channel.BasicConsumeAsync(_queueName, false, consumer);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (_channel is not null)
            {
                try
                {
                    if (_consumerTag is not null)
                    {
                        _channel.BasicCancelAsync(_consumerTag).GetAwaiter().GetResult();
                    }

                    _channel.CloseAsync().GetAwaiter().GetResult();
                    _channel.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch
                {
                    // Best-effort teardown.
                }
            }

            _provider._metrics.SetSubscriberCount(
                _queueName,
                _provider._subscriberCounts.AddOrUpdate(_queueName, 0, static (_, count) => Math.Max(0, count - 1))
            );
        }
    }
}
