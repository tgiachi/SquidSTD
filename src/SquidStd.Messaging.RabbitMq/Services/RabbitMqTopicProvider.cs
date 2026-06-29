using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.RabbitMq.Data.Config;

namespace SquidStd.Messaging.RabbitMq.Services;

/// <summary>
///     RabbitMQ <see cref="ITopicProvider" />: topics map to fanout exchanges; each subscriber binds an
///     exclusive auto-delete queue and consumes with auto-ack (transient, at-most-once fan-out).
/// </summary>
public sealed class RabbitMqTopicProvider : ITopicProvider
{
    private readonly HashSet<string> _declared = new(StringComparer.Ordinal);
    private readonly Lock _exchangeSync = new();
    private readonly ILogger _logger = Log.ForContext<RabbitMqTopicProvider>();
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private IConnection? _connection;
    private int _disposed;
    private IChannel? _publishChannel;

    public RabbitMqTopicProvider(RabbitMqOptions options)
    {
        _options = options;
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
    public async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        var channel = _publishChannel ?? throw new InvalidOperationException("Provider not started.");

        await EnsureExchangeAsync(channel, topic, cancellationToken);

        var properties = new BasicProperties();

        await _publishLock.WaitAsync(cancellationToken);

        try
        {
            await channel.BasicPublishAsync(
                topic,
                string.Empty,
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
    public IDisposable Subscribe(string topic, Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var connection = _connection ?? throw new InvalidOperationException("Provider not started.");
        var subscription = new Subscription(this, connection, topic, handler);
        subscription.Start();

        return subscription;
    }

    private async Task EnsureExchangeAsync(IChannel channel, string topic, CancellationToken cancellationToken)
    {
        lock (_exchangeSync)
        {
            if (!_declared.Add(topic))
            {
                return;
            }
        }

        try
        {
            await channel.ExchangeDeclareAsync(
                topic,
                ExchangeType.Fanout,
                false,
                false,
                cancellationToken: cancellationToken
            );
        }
        catch
        {
            // The declare failed, so the exchange is not actually established: undo the optimistic
            // mark so a later call retries instead of assuming the topology exists.
            lock (_exchangeSync)
            {
                _declared.Remove(topic);
            }

            throw;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly IConnection _connection;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _handler;
        private readonly RabbitMqTopicProvider _provider;
        private readonly string _topic;
        private IChannel? _channel;
        private string? _consumerTag;
        private int _disposed;

        public Subscription(
            RabbitMqTopicProvider provider,
            IConnection connection,
            string topic,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler
        )
        {
            _provider = provider;
            _connection = connection;
            _topic = topic;
            _handler = handler;
        }

        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                await _handler(args.Body, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _provider._logger.Warning(ex, "RabbitMq topic '{Topic}' handler failed", _topic);
            }
        }

        private async Task StartAsync()
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(_topic, ExchangeType.Fanout, false, false);

            var queue = await _channel.QueueDeclareAsync(string.Empty, false, true, true);
            await _channel.QueueBindAsync(queue.QueueName, _topic, string.Empty);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnReceivedAsync;

            _consumerTag = await _channel.BasicConsumeAsync(queue.QueueName, true, consumer);
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
        }
    }
}
