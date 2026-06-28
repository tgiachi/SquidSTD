using System.Collections.Concurrent;
using SquidStd.Core.Data.Metrics;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Types.Metrics;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Messaging.Abstractions.Services;

/// <summary>
///     Accumulates messaging metrics and exposes them to the metrics collection system.
/// </summary>
public sealed class MessagingMetricsProvider : IMessagingMetrics, IMetricProvider
{
    private readonly ConcurrentDictionary<string, QueueCounters> _queues = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string ProviderName => "messaging";

    /// <inheritdoc />
    public void OnDeadLettered(string queueName)
    {
        Interlocked.Increment(ref Counters(queueName).DeadLettered);
    }

    /// <inheritdoc />
    public void OnDelivered(string queueName)
    {
        Interlocked.Increment(ref Counters(queueName).Delivered);
    }

    /// <inheritdoc />
    public void OnFailed(string queueName)
    {
        Interlocked.Increment(ref Counters(queueName).Failed);
    }

    /// <inheritdoc />
    public void OnPublished(string queueName)
    {
        Interlocked.Increment(ref Counters(queueName).Published);
    }

    /// <inheritdoc />
    public void OnRetried(string queueName)
    {
        Interlocked.Increment(ref Counters(queueName).Retried);
    }

    /// <inheritdoc />
    public void SetQueueDepth(string queueName, int depth)
    {
        Volatile.Write(ref Counters(queueName).Depth, depth);
    }

    /// <inheritdoc />
    public void SetSubscriberCount(string queueName, int count)
    {
        Volatile.Write(ref Counters(queueName).Subscribers, count);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<MetricSample>> CollectAsync(CancellationToken cancellationToken = default)
    {
        var samples = new List<MetricSample>();

        foreach (var (queueName, counters) in _queues)
        {
            var tags = new Dictionary<string, string>(StringComparer.Ordinal) { ["queue"] = queueName };

            samples.Add(
                new MetricSample("published", Interlocked.Read(ref counters.Published), Tags: tags, Type: MetricType.Counter)
            );
            samples.Add(
                new MetricSample("delivered", Interlocked.Read(ref counters.Delivered), Tags: tags, Type: MetricType.Counter)
            );
            samples.Add(
                new MetricSample("failed", Interlocked.Read(ref counters.Failed), Tags: tags, Type: MetricType.Counter)
            );
            samples.Add(
                new MetricSample("retried", Interlocked.Read(ref counters.Retried), Tags: tags, Type: MetricType.Counter)
            );
            samples.Add(
                new MetricSample(
                    "dead_lettered",
                    Interlocked.Read(ref counters.DeadLettered),
                    Tags: tags,
                    Type: MetricType.Counter
                )
            );
            samples.Add(new MetricSample("queue_depth", Volatile.Read(ref counters.Depth), Tags: tags));
            samples.Add(new MetricSample("subscribers", Volatile.Read(ref counters.Subscribers), Tags: tags));
        }

        return ValueTask.FromResult<IReadOnlyList<MetricSample>>(samples);
    }

    private QueueCounters Counters(string queueName)
    {
        return _queues.GetOrAdd(queueName, static _ => new QueueCounters());
    }

    private sealed class QueueCounters
    {
        public long DeadLettered;
        public long Delivered;
        public int Depth;
        public long Failed;
        public long Published;
        public long Retried;
        public int Subscribers;
    }
}
