using SquidStd.Core.Data.Bootstrap;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Data.Config;
using SquidStd.Mail.Abstractions.Data.Events;
using SquidStd.Mail.Abstractions.Interfaces;
using SquidStd.Mail.Abstractions.Types.Mail;
using SquidStd.Mail.MailKit.Extensions;
using SquidStd.Mail.Queue.Extensions;
using SquidStd.Mail.Queue.Interfaces;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services.Bootstrap;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

#region step-1
bootstrap.ConfigureServices(container => container.AddMail(new MailOptions
{
    Protocol = MailProtocolType.Imap,
    Host = "imap.example.com",
    Port = 993,
    Username = "alice@example.com",
    Password = "app-password"
}));
#endregion

#region step-2
bootstrap.ConfigureServices(container => container.AddMailSender(new SmtpOptions
{
    Host = "smtp.example.com",
    Port = 587
}));
#endregion

#region step-3
bootstrap.ConfigureServices(container => container
    .AddInMemoryMessaging()
    .AddMailQueue());
#endregion

await bootstrap.StartAsync();

// Inbound: react to each received email on the event bus.
var eventBus = bootstrap.Resolve<IEventBus>();
eventBus.RegisterAsyncListener(new MailReceivedLogger());

var outgoing = new OutgoingMailMessage
{
    To = [new MailAddress("Bob", "bob@example.com")],
    Subject = "Hi",
    HtmlBody = "<p>Hi</p>"
};

// Outbound: queue for background sending (no network call).
var queue = bootstrap.Resolve<IMailQueue>();
await queue.EnqueueAsync(outgoing);

// Or send synchronously; guarded so the sample runs without a live SMTP server.
if (args.Contains("--send"))
{
    var sender = bootstrap.Resolve<IMailSender>();
    await sender.SendAsync(outgoing);
}

await bootstrap.StopAsync();

/// <summary>Logs every received email as it arrives on the event bus.</summary>
public sealed class MailReceivedLogger : IAsyncEventListener<MailReceivedEvent>
{
    /// <summary>Handles a received-mail event.</summary>
    public Task HandleAsync(MailReceivedEvent eventData, CancellationToken cancellationToken)
    {
        Console.WriteLine($"received: {eventData.Message.Subject}");

        return Task.CompletedTask;
    }
}
