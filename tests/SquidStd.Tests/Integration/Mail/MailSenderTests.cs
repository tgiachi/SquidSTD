using System.Text;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Services.Core.Services;
using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Data.Config;
using SquidStd.Mail.Abstractions.Data.Events;
using SquidStd.Mail.Abstractions.Exceptions;
using SquidStd.Mail.Abstractions.Types.Mail;
using SquidStd.Mail.MailKit.Services;

namespace SquidStd.Tests.Integration.Mail;

[Collection(GreenMailCollection.Name)]
public class MailSenderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly GreenMailContainerFixture _fixture;

    public MailSenderTests(GreenMailContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed class DelegateListener<TEvent> : IAsyncEventListener<TEvent>
        where TEvent : IEvent
    {
        private readonly Action<TEvent> _onEvent;

        public DelegateListener(Action<TEvent> onEvent)
        {
            _onEvent = onEvent;
        }

        public Task HandleAsync(TEvent eventData, CancellationToken cancellationToken)
        {
            _onEvent(eventData);

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SendAsync_DeliversMessage_AndPublishesMailSent()
    {
        var recipient = "send-target@example.com";
        var eventBus = new EventBusService();
        var sent = new TaskCompletionSource<MailSentEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        eventBus.RegisterAsyncListener(new DelegateListener<MailSentEvent>(e => sent.TrySetResult(e)));

        var sender = new MailKitMailSender(
            new SmtpOptions { Host = _fixture.Host, Port = _fixture.SmtpPort, UseSsl = false },
            eventBus);

        await sender.SendAsync(new OutgoingMailMessage
        {
            From = new MailAddress("Sender", "sender@example.com"),
            To = [new MailAddress("Target", recipient)],
            Subject = "sent-subject",
            TextBody = "hello",
            Attachments = [new OutgoingAttachment("a.txt", "text/plain", Encoding.UTF8.GetBytes("xyz"))]
        }).WaitAsync(Timeout);

        var sentEvent = await sent.Task.WaitAsync(Timeout);
        Assert.Equal("sent-subject", sentEvent.Subject);

        var reader = new ImapMailReader(new MailOptions
        {
            Protocol = MailProtocolType.Imap,
            Host = _fixture.Host,
            Port = _fixture.ImapPort,
            UseSsl = false,
            Username = recipient,
            Password = "pwd"
        });

        var received = await reader.FetchNewAsync().WaitAsync(Timeout);
        Assert.Contains(received, m => m.Subject == "sent-subject");
    }

    [Fact]
    public async Task SendAsync_Throws_AndPublishesFailed_OnClosedPort()
    {
        var eventBus = new EventBusService();
        var failed = new TaskCompletionSource<MailSendFailedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        eventBus.RegisterAsyncListener(new DelegateListener<MailSendFailedEvent>(e => failed.TrySetResult(e)));

        var sender = new MailKitMailSender(new SmtpOptions { Host = _fixture.Host, Port = 1, UseSsl = false }, eventBus);

        await Assert.ThrowsAsync<MailSendException>(() => sender.SendAsync(new OutgoingMailMessage
        {
            From = new MailAddress("Sender", "sender@example.com"),
            To = [new MailAddress("Target", "x@example.com")],
            Subject = "fail-subject"
        }).WaitAsync(Timeout));

        var failedEvent = await failed.Task.WaitAsync(Timeout);
        Assert.Equal("fail-subject", failedEvent.Subject);
    }
}
