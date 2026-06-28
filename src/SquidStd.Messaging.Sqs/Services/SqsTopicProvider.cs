using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Serilog;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Sqs.Data.Config;
using SquidStd.Messaging.Sqs.Internal;

namespace SquidStd.Messaging.Sqs.Services;

/// <summary>
///     SNS+SQS <see cref="ITopicProvider" />: a topic is an SNS topic; each subscriber gets a dedicated
///     ephemeral SQS queue subscribed to the topic with raw message delivery, long-polled and torn down
///     on dispose (transient, at-most-once fan-out). Payloads travel base64-encoded.
/// </summary>
public sealed class SqsTopicProvider : ITopicProvider
{
    private readonly ILogger _logger = Log.ForContext<SqsTopicProvider>();
    private readonly SqsOptions _options;
    private readonly ConcurrentDictionary<string, string> _topicArns = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _topicLock = new(1, 1);
    private int _disposed;
    private IAmazonSimpleNotificationService? _sns;
    private IAmazonSQS? _sqs;
    private int _subscriberSeq;

    public SqsTopicProvider(SqsOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _sqs?.Dispose();
        _sns?.Dispose();
        _topicLock.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        var sns = _sns ?? throw new InvalidOperationException("Provider not started.");

        var arn = await EnsureTopicAsync(topic, cancellationToken);

        await sns.PublishAsync(
            new PublishRequest { TopicArn = arn, Message = Convert.ToBase64String(payload.Span) },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        var credentials = AwsClientFactory.Credentials(_options.Aws);
        _sqs = new AmazonSQSClient(credentials, AwsClientFactory.SqsConfig(_options.Aws));
        _sns = new AmazonSimpleNotificationServiceClient(credentials, AwsClientFactory.SnsConfig(_options.Aws));

        return ValueTask.CompletedTask;
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

        if (_sqs is null || _sns is null)
        {
            throw new InvalidOperationException("Provider not started.");
        }

        var index = Interlocked.Increment(ref _subscriberSeq);
        var subscription = new Subscription(this, topic, index, handler);
        subscription.Start();

        return subscription;
    }

    private async Task<string> EnsureTopicAsync(string topic, CancellationToken cancellationToken)
    {
        var name = SqsNames.Sanitize(topic);

        if (_topicArns.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var sns = _sns ?? throw new InvalidOperationException("Provider not started.");

        await _topicLock.WaitAsync(cancellationToken);

        try
        {
            if (_topicArns.TryGetValue(name, out cached))
            {
                return cached;
            }

            var arn = (await sns.CreateTopicAsync(new CreateTopicRequest { Name = name }, cancellationToken)).TopicArn;
            _topicArns[name] = arn;

            return arn;
        }
        finally
        {
            _topicLock.Release();
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _handler;
        private readonly int _index;
        private readonly SqsTopicProvider _provider;
        private readonly string _topic;
        private int _disposed;
        private Task? _loop;
        private string? _queueUrl;
        private string? _subscriptionArn;

        public Subscription(
            SqsTopicProvider provider,
            string topic,
            int index,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> handler
        )
        {
            _provider = provider;
            _topic = topic;
            _index = index;
            _handler = handler;
        }

        public void Start()
        {
            _loop = Task.Run(() => RunAsync(_cts.Token));
        }

        private static string BuildPolicy(string queueArn, string topicArn)
        {
            return JsonSerializer.Serialize(
                new
                {
                    Version = "2012-10-17",
                    Statement = new[]
                    {
                        new
                        {
                            Effect = "Allow",
                            Principal = new { Service = "sns.amazonaws.com" },
                            Action = "sqs:SendMessage",
                            Resource = queueArn,
                            Condition = new { ArnEquals = new Dictionary<string, string> { ["aws:SourceArn"] = topicArn } }
                        }
                    }
                }
            );
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            string queueUrl;

            try
            {
                var topicArn = await _provider.EnsureTopicAsync(_topic, cancellationToken);
                var queueName = SqsNames.Sanitize(_topic) + "-sub-" + _index;
                queueUrl = (await _provider._sqs!.CreateQueueAsync(
                    new CreateQueueRequest { QueueName = queueName },
                    cancellationToken
                )).QueueUrl;
                _queueUrl = queueUrl;

                var attributes = await _provider._sqs.GetQueueAttributesAsync(
                    new GetQueueAttributesRequest { QueueUrl = queueUrl, AttributeNames = ["QueueArn"] },
                    cancellationToken
                );
                var queueArn = attributes.Attributes["QueueArn"];

                await _provider._sqs.SetQueueAttributesAsync(
                    new SetQueueAttributesRequest
                    {
                        QueueUrl = queueUrl,
                        Attributes = new Dictionary<string, string> { ["Policy"] = BuildPolicy(queueArn, topicArn) }
                    },
                    cancellationToken
                );

                _subscriptionArn = (await _provider._sns!.SubscribeAsync(
                    new SubscribeRequest
                    {
                        TopicArn = topicArn,
                        Protocol = "sqs",
                        Endpoint = queueArn,
                        ReturnSubscriptionArn = true,
                        Attributes = new Dictionary<string, string> { ["RawMessageDelivery"] = "true" }
                    },
                    cancellationToken
                )).SubscriptionArn;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _provider._logger.Warning(ex, "SQS topic '{Topic}' subscribe setup failed", _topic);

                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                ReceiveMessageResponse response;

                try
                {
                    response = await _provider._sqs!.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = queueUrl,
                            MaxNumberOfMessages = _provider._options.MaxNumberOfMessages,
                            WaitTimeSeconds = _provider._options.WaitTimeSeconds
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
                    _provider._logger.Warning(ex, "SQS topic '{Topic}' receive failed", _topic);

                    continue;
                }

                foreach (var message in response.Messages ?? [])
                {
                    try
                    {
                        var payload = Convert.FromBase64String(message.Body);
                        await _handler(payload, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _provider._logger.Warning(ex, "SQS topic '{Topic}' handler failed", _topic);
                    }
                    finally
                    {
                        try
                        {
                            await _provider._sqs!.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                        }
                        catch
                        {
                            // Best-effort ack.
                        }
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

                if (_subscriptionArn is not null)
                {
                    _provider._sns!.UnsubscribeAsync(_subscriptionArn).GetAwaiter().GetResult();
                }

                if (_queueUrl is not null)
                {
                    _provider._sqs!.DeleteQueueAsync(_queueUrl).GetAwaiter().GetResult();
                }
            }
            catch
            {
                // Best-effort teardown.
            }

            _cts.Dispose();
        }
    }
}
