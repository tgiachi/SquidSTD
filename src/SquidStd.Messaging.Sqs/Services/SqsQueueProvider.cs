using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Serilog;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Abstractions.Services;
using SquidStd.Messaging.Sqs.Data.Config;
using SquidStd.Messaging.Sqs.Internal;

namespace SquidStd.Messaging.Sqs.Services;

/// <summary>
///     AWS SQS <see cref="IQueueProvider" />: named queues are created with a redrive policy to a
///     "&lt;queue&gt;&lt;suffix&gt;" dead-letter queue (maxReceiveCount = MaxDeliveryAttempts). Subscribers
///     long-poll; a handler that throws leaves the message un-acked so SQS redelivers and eventually
///     dead-letters it. Payloads travel base64-encoded in the message body.
/// </summary>
public sealed class SqsQueueProvider : IQueueProvider
{
    private readonly string _deadLetterSuffix;
    private readonly ILogger _logger = Log.ForContext<SqsQueueProvider>();
    private readonly MessagingOptions _messagingOptions;
    private readonly IMessagingMetrics _metrics;
    private readonly SqsOptions _options;
    private readonly ConcurrentDictionary<string, string> _queueUrls = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _subscriberCounts = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _topologyLock = new(1, 1);
    private IAmazonSQS? _client;
    private int _disposed;

    public SqsQueueProvider(SqsOptions options, MessagingOptions messagingOptions, IMessagingMetrics? metrics = null)
    {
        _options = options;
        _messagingOptions = messagingOptions;
        _metrics = metrics ?? NoOpMessagingMetrics.Instance;
        _deadLetterSuffix = SqsNames.Sanitize(messagingOptions.DeadLetterQueueSuffix);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _client?.Dispose();
        _topologyLock.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        string queueName,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        var client = _client ?? throw new InvalidOperationException("Provider not started.");

        var url = await EnsureQueueAsync(queueName, cancellationToken);

        await client.SendMessageAsync(
            new SendMessageRequest { QueueUrl = url, MessageBody = Convert.ToBase64String(payload.Span) },
            cancellationToken
        );

        _metrics.OnPublished(queueName);
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _client = new AmazonSQSClient(AwsClientFactory.Credentials(_options.Aws), AwsClientFactory.SqsConfig(_options.Aws));

        return ValueTask.CompletedTask;
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
        var client = _client ?? throw new InvalidOperationException("Provider not started.");

        var subscription = new Subscription(this, client, queueName, handler);
        subscription.Start();
        _metrics.SetSubscriberCount(queueName, _subscriberCounts.AddOrUpdate(queueName, 1, static (_, count) => count + 1));

        return subscription;
    }

    private async Task<string> EnsureQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var name = SqsNames.Sanitize(queueName);

        if (_queueUrls.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var client = _client ?? throw new InvalidOperationException("Provider not started.");

        await _topologyLock.WaitAsync(cancellationToken);

        try
        {
            if (_queueUrls.TryGetValue(name, out cached))
            {
                return cached;
            }

            string url;

            if (name.EndsWith(_deadLetterSuffix, StringComparison.Ordinal))
            {
                url = (await client.CreateQueueAsync(new CreateQueueRequest { QueueName = name }, cancellationToken))
                    .QueueUrl;
            }
            else
            {
                var dlqName = name + _deadLetterSuffix;
                var dlqUrl = (await client.CreateQueueAsync(
                    new CreateQueueRequest { QueueName = dlqName },
                    cancellationToken
                )).QueueUrl;
                var dlqArn = await GetQueueArnAsync(client, dlqUrl, cancellationToken);

                var redrivePolicy = JsonSerializer.Serialize(
                    new Dictionary<string, string>
                    {
                        ["deadLetterTargetArn"] = dlqArn,
                        ["maxReceiveCount"] = _messagingOptions.MaxDeliveryAttempts.ToString(CultureInfo.InvariantCulture)
                    }
                );

                url = (await client.CreateQueueAsync(
                    new CreateQueueRequest
                    {
                        QueueName = name,
                        Attributes = new Dictionary<string, string> { ["RedrivePolicy"] = redrivePolicy }
                    },
                    cancellationToken
                )).QueueUrl;
            }

            _queueUrls[name] = url;

            return url;
        }
        finally
        {
            _topologyLock.Release();
        }
    }

    private static async Task<string> GetQueueArnAsync(
        IAmazonSQS client,
        string queueUrl,
        CancellationToken cancellationToken
    )
    {
        var response = await client.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["QueueArn"] },
            cancellationToken
        );

        return response.Attributes["QueueArn"];
    }

    private sealed class Subscription : IDisposable
    {
        private readonly IAmazonSQS _client;
        private readonly CancellationTokenSource _cts = new();
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _handler;
        private readonly SqsQueueProvider _provider;
        private readonly string _queueName;
        private int _disposed;
        private Task? _loop;

        public Subscription(
            SqsQueueProvider provider,
            IAmazonSQS client,
            string queueName,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler
        )
        {
            _provider = provider;
            _client = client;
            _queueName = queueName;
            _handler = handler;
        }

        public void Start()
        {
            _loop = Task.Run(() => RunAsync(_cts.Token));
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            string url;

            try
            {
                url = await _provider.EnsureQueueAsync(_queueName, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _provider._logger.Warning(ex, "SQS subscribe setup failed for {QueueName}", _queueName);

                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                ReceiveMessageResponse response;

                try
                {
                    response = await _client.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = url,
                            MaxNumberOfMessages = _provider._options.MaxNumberOfMessages,
                            WaitTimeSeconds = _provider._options.WaitTimeSeconds,
                            VisibilityTimeout = (int)_provider._options.VisibilityTimeout.TotalSeconds
                        },
                        cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _provider._logger.Warning(ex, "SQS receive failed for {QueueName}", _queueName);

                    continue;
                }

                foreach (var message in response.Messages ?? [])
                {
                    try
                    {
                        var payload = Convert.FromBase64String(message.Body);
                        await _handler(payload, cancellationToken);
                        await _client.DeleteMessageAsync(url, message.ReceiptHandle, cancellationToken);
                        _provider._metrics.OnDelivered(_queueName);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _provider._logger.Warning(ex, "SQS handler failed for {QueueName}", _queueName);
                        _provider._metrics.OnFailed(_queueName);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _cts.Cancel();

            try
            {
                _loop?.GetAwaiter().GetResult();
            }
            catch
            {
                // Best-effort teardown.
            }

            _cts.Dispose();
            _provider._metrics.SetSubscriberCount(
                _queueName,
                _provider._subscriberCounts.AddOrUpdate(_queueName, 0, static (_, count) => Math.Max(0, count - 1))
            );
        }
    }
}
