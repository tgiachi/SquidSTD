using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Data.Config;
using SquidStd.Mail.Abstractions.Types.Mail;
using SquidStd.Mail.MailKit.Extensions;
using SquidStd.Mail.MailKit.Services;
using SquidStd.Mail.Queue.Extensions;
using SquidStd.Mail.Queue.Interfaces;
using SquidStd.Mail.Queue.Services;

namespace SquidStd.Tests.Integration.Mail;

[Collection(GreenMailCollection.Name)]
public class MailQueueIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly GreenMailContainerFixture _fixture;

    public MailQueueIntegrationTests(GreenMailContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Enqueue_IsSentByConsumer_AndDelivered()
    {
        var recipient = "queue-target@example.com";

        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging();
        container.AddMailSender(new SmtpOptions { Host = _fixture.Host, Port = _fixture.SmtpPort, UseSsl = false });
        container.AddMailQueue();

        var consumer = container.Resolve<MailSendConsumerService>();
        await consumer.StartAsync();

        await container.Resolve<IMailQueue>().EnqueueAsync(new OutgoingMailMessage
        {
            From = new MailAddress("Sender", "sender@example.com"),
            To = [new MailAddress("Target", recipient)],
            Subject = "queued-subject",
            TextBody = "hi"
        });

        var reader = new ImapMailReader(new MailOptions
        {
            Protocol = MailProtocolType.Imap,
            Host = _fixture.Host,
            Port = _fixture.ImapPort,
            UseSsl = false,
            Username = recipient,
            Password = "pwd"
        });

        IReadOnlyList<MailMessage> received = [];
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline && received.Count == 0)
        {
            received = await reader.FetchNewAsync().WaitAsync(Timeout);
            if (received.Count == 0)
            {
                await Task.Delay(500);
            }
        }

        await consumer.StopAsync();

        Assert.Contains(received, m => m.Subject == "queued-subject");
    }
}
