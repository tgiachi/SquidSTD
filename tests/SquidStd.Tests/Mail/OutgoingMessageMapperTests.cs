using System.Text;
using MimeKit;
using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Data.Config;
using SquidStd.Mail.MailKit.Services;

namespace SquidStd.Tests.Mail;

public class OutgoingMessageMapperTests
{
    private static OutgoingMailMessage Message(MailAddress? from = null)
        => new()
        {
            From = from,
            To = [new MailAddress("Bob", "bob@example.com")],
            Cc = [new MailAddress("Carol", "carol@example.com")],
            Bcc = [new MailAddress("Dave", "dave@example.com")],
            Subject = "Hello",
            TextBody = "plain",
            HtmlBody = "<p>html</p>",
            Attachments = [new OutgoingAttachment("a.txt", "text/plain", Encoding.UTF8.GetBytes("xyz"))]
        };

    [Fact]
    public void ToMimeMessage_MapsRecipientsSubjectBodiesAndAttachment()
    {
        var options = new SmtpOptions { DefaultFrom = new MailAddress("Sys", "sys@example.com") };

        var mime = OutgoingMessageMapper.ToMimeMessage(Message(new MailAddress("Alice", "alice@example.com")), options);

        Assert.Equal("alice@example.com", mime.From.Mailboxes.Single().Address);
        Assert.Contains(mime.To.Mailboxes, m => m.Address == "bob@example.com");
        Assert.Contains(mime.Cc.Mailboxes, m => m.Address == "carol@example.com");
        Assert.Contains(mime.Bcc.Mailboxes, m => m.Address == "dave@example.com");
        Assert.Equal("Hello", mime.Subject);
        Assert.Equal("plain", mime.TextBody);
        Assert.Equal("<p>html</p>", mime.HtmlBody);
        Assert.Contains(mime.Attachments.OfType<MimePart>(), p => p.FileName == "a.txt");
    }

    [Fact]
    public void ToMimeMessage_FallsBackToDefaultFrom()
    {
        var options = new SmtpOptions { DefaultFrom = new MailAddress("Sys", "sys@example.com") };

        var mime = OutgoingMessageMapper.ToMimeMessage(Message(from: null), options);

        Assert.Equal("sys@example.com", mime.From.Mailboxes.Single().Address);
    }

    [Fact]
    public void ToMimeMessage_Throws_WhenNoFromAndNoDefault()
    {
        Assert.Throws<ArgumentException>(() => OutgoingMessageMapper.ToMimeMessage(Message(from: null), new SmtpOptions()));
    }
}
