using System.Net.Sockets;
using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Interfaces.Services;
using Infrastructure.Settings;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Services;

/// <summary>Sends through Gmail's SMTP server (MailKit), logged in with a Gmail OAuth2 access token.</summary>
public sealed class GmailSmtpEmailSender(IGmailAuthService authService, IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new TextPart(TextFormat.Html) { Text = message.HtmlBody };

        var accessToken = await authService.GetAccessTokenAsync();
        using var smtp = new SmtpClient();
        try
        {
            await smtp.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port, SecureSocketOptions.StartTls,
                cancellationToken);
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException)
        {
            throw new MailServerUnavailableException(
                $"The mail server {_settings.Smtp.Host}:{_settings.Smtp.Port} can't be reached.", ex);
        }

        await smtp.AuthenticateAsync(new SaslMechanismOAuth2(_settings.FromAddress, accessToken), cancellationToken);
        await smtp.SendAsync(mime, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}
