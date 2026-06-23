using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Tests.Workers.Support;

/// <summary>Inert <see cref="IMessageQueue" /> for unit tests that drive the consumer's HandleAsync directly.</summary>
public sealed class FakeMessageQueue : IMessageQueue
{
    public Task PublishAsync<TMessage>(string queueName, TMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public IDisposable Subscribe<TMessage>(string queueName, IQueueMessageListener<TMessage> listener)
        => new Subscription();

    public IDisposable Subscribe<TMessage>(string queueName, IQueueMessageListenerAsync<TMessage> listener)
        => new Subscription();

    private sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
