using Application.Common.Models;

namespace Application.Interfaces.Services;

/// <summary>
/// Sends one email through the configured provider (<c>Email:Provider</c>). It returns once the mail server has
/// accepted the message, and throws <see cref="Common.Exceptions.MailServerUnavailableException"/> when it can't be
/// reached.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
