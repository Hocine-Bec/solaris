using Application.Interfaces.Services;
using Infrastructure.Services.Extensions;
using Infrastructure.Settings;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Services;

public class EmailService(IGmailAuthService authService, IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _setting = options.Value;
    private readonly IGmailAuthService _authService = authService;

    public async Task SendLeadEmailAsync(string toEmail, string toName, string propertyAddress)
    {
        const string subject = "Your Solar Quote Request - We're on it! ☀️";
        var htmlBody = EmailTemplate.GetLeadWelcomeTemplate(toName, propertyAddress);
        var message = CreateEmailMessage(toEmail, toName, subject, htmlBody);
        await SendEmailAsync(message);
    }

    public async Task SendSalesNotificationEmailAsync(int toCustomerId, string toName, string toEmail,
        string toPhone, string toAddress)
    {
        var subject = $"🔔 New Lead: {toName}";
        var htmlBody = EmailTemplate.GetSalesNotificationTemplate(toCustomerId, toName, toEmail, toPhone, toAddress);
        var message = CreateEmailMessage(_setting.FromAddress, _setting.FromName, subject, htmlBody);
        await SendEmailAsync(message);
    }
    
    private MimeMessage CreateEmailMessage(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_setting.FromName, _setting.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };
        return message;
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        var accessToken = await _authService.GetAccessTokenAsync();
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_setting.Smtp.Host, _setting.Smtp.Port, SecureSocketOptions.StartTls);
        var oauth2 = new SaslMechanismOAuth2(_setting.FromAddress, accessToken);
        await smtp.AuthenticateAsync(oauth2);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}