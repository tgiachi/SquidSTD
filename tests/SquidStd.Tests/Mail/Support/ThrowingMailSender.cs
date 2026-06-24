using SquidStd.Mail.Abstractions.Data;
using SquidStd.Mail.Abstractions.Interfaces;

namespace SquidStd.Tests.Mail.Support;

/// <summary>Test <see cref="IMailSender" /> that always throws, to exercise the retry/dead-letter path.</summary>
public sealed class ThrowingMailSender : IMailSender
{
    public Task SendAsync(OutgoingMailMessage message, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("send failed");
}
