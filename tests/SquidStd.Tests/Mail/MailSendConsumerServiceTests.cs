using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Data.Config;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Tests.Mail.Support;
using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Interfaces;
using SquidStd.Mail.Queue.Data.Config;
using SquidStd.Mail.Queue.Services;

namespace SquidStd.Tests.Mail;

public class MailSendConsumerServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static OutgoingMailMessage NewMessage()
        => new() { To = [new MailAddress("Bob", "bob@example.com")], Subject = "queued" };

    [Fact]
    public async Task Consumer_SendsEnqueuedMessage()
    {
        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging();
        var sender = new RecordingMailSender();
        container.RegisterInstance<IMailSender>(sender);

        var options = new MailQueueOptions();
        var queue = container.Resolve<IMessageQueue>();
        var consumer = new MailSendConsumerService(queue, sender, options);
        await consumer.StartAsync();

        await new MailQueue(queue, options).EnqueueAsync(NewMessage());

        await WaitUntilAsync(() => sender.Sent.Count == 1, Timeout);
        await consumer.StopAsync();

        Assert.Single(sender.Sent);
        Assert.Equal("queued", sender.Sent[0].Subject);
    }

    [Fact]
    public async Task Consumer_DeadLetters_AfterRetriesExhausted()
    {
        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging(new MessagingOptions { MaxDeliveryAttempts = 2, RetryDelay = TimeSpan.Zero });
        var sender = new ThrowingMailSender();
        container.RegisterInstance<IMailSender>(sender);

        var options = new MailQueueOptions();
        var queue = container.Resolve<IMessageQueue>();

        var deadLettered = new TaskCompletionSource<OutgoingMailMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var _ = queue.Subscribe<OutgoingMailMessage>(options.QueueName + ".dlq", new CapturingListener(deadLettered));

        var consumer = new MailSendConsumerService(queue, sender, options);
        await consumer.StartAsync();

        await new MailQueue(queue, options).EnqueueAsync(NewMessage());

        var dead = await deadLettered.Task.WaitAsync(Timeout);
        await consumer.StopAsync();

        Assert.Equal("queued", dead.Subject);
    }

    private sealed class CapturingListener : IQueueMessageListenerAsync<OutgoingMailMessage>
    {
        private readonly TaskCompletionSource<OutgoingMailMessage> _completion;

        public CapturingListener(TaskCompletionSource<OutgoingMailMessage> completion)
        {
            _completion = completion;
        }

        public Task HandleAsync(OutgoingMailMessage message, CancellationToken cancellationToken)
        {
            _completion.TrySetResult(message);

            return Task.CompletedTask;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Condition not met within timeout.");
    }
}
