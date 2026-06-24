using MailKit.Net.Smtp;
using MailKit.Security;
using Serilog;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Data.Config;
using SquidStd.Mail.Abstractions.Data.Events;
using SquidStd.Mail.Abstractions.Exceptions;
using SquidStd.Mail.Abstractions.Interfaces;

namespace SquidStd.Mail.MailKit.Services;

/// <summary>MailKit <see cref="IMailSender" />: sends via SMTP and publishes send events.</summary>
public sealed class MailKitMailSender : IMailSender
{
    private readonly ILogger _logger = Log.ForContext<MailKitMailSender>();
    private readonly SmtpOptions _options;
    private readonly IEventBus _eventBus;

    public MailKitMailSender(SmtpOptions options, IEventBus eventBus)
    {
        _options = options;
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public async Task SendAsync(OutgoingMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.To.Count == 0)
        {
            throw new ArgumentException("At least one recipient (To) is required.", nameof(message));
        }

        var mime = OutgoingMessageMapper.ToMimeMessage(message, _options);

        try
        {
            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(
                    _options.Host,
                    _options.Port,
                    _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
                }

                await client.SendAsync(mime, cancellationToken);
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true, CancellationToken.None);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await PublishSafelyAsync(new MailSendFailedEvent(message.To, message.Subject, ex.Message), CancellationToken.None);
            _logger.Error(ex, "Failed to send mail '{Subject}'.", message.Subject);

            throw new MailSendException($"Failed to send mail '{message.Subject}'.", ex);
        }

        await PublishSafelyAsync(new MailSentEvent(message.To, message.Subject), cancellationToken);
    }

    private async Task PublishSafelyAsync<TEvent>(TEvent payload, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        try
        {
            await _eventBus.PublishAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to publish {Event}.", typeof(TEvent).Name);
        }
    }
}
