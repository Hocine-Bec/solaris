using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Interfaces.Services;

namespace Infrastructure.Measurement;

/// <summary>While the fault is on, every send fails as if the mail server were unreachable, without reaching it.</summary>
public sealed class FaultInjectingEmailSender(IEmailSender inner, EmailFaultSwitch fault) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (fault.IsOn)
            throw new MailServerUnavailableException("The mail server is unreachable (fault injected for a measurement).");

        await inner.SendAsync(message, cancellationToken);
    }
}
