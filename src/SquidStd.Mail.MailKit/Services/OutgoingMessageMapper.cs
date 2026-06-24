using MimeKit;
using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Data.Config;

namespace SquidStd.Mail.MailKit.Services;

/// <summary>Maps a SquidStd <see cref="OutgoingMailMessage" /> to a MimeKit <see cref="MimeMessage" />.</summary>
public static class OutgoingMessageMapper
{
    /// <summary>Builds a MIME message; <c>From</c> falls back to <see cref="SmtpOptions.DefaultFrom" />.</summary>
    public static MimeMessage ToMimeMessage(OutgoingMailMessage message, SmtpOptions options)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);

        var from = message.From ?? options.DefaultFrom
            ?? throw new ArgumentException("No sender: set OutgoingMailMessage.From or SmtpOptions.DefaultFrom.", nameof(message));

        var mime = new MimeMessage();
        mime.From.Add(ToMailbox(from));
        mime.To.AddRange(message.To.Select(ToMailbox));
        mime.Cc.AddRange(message.Cc.Select(ToMailbox));
        mime.Bcc.AddRange(message.Bcc.Select(ToMailbox));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody
        };

        foreach (var attachment in message.Attachments)
        {
            builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }

        mime.Body = builder.ToMessageBody();

        return mime;
    }

    private static MailboxAddress ToMailbox(MailAddress address)
        => new(address.Name, address.Address);
}
