using Application.Interfaces.Services;
using Domain.ValueObjects;
using Infrastructure.Services.Extensions;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly GmailSetting _setting = new();    
    private readonly IGmailAuthService _authService;
    
    public EmailService(IGmailAuthService authService)
    {
        _authService = authService;
        _setting.SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? throw new InvalidOperationException("SMTP_HOST environment variable is not set");
        _setting.SmtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? throw new InvalidOperationException("SMTP_PORT environment variable is not set"));
        _setting.FromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? throw new InvalidOperationException("SMTP_FROM_NAME environment variable is not set");
        _setting.FromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? throw new InvalidOperationException("SMTP_FROM_EMAIL environment variable is not set");
    }

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
        var message = CreateEmailMessage(_setting.FromEmail, _setting.FromName, subject, htmlBody);
        await SendEmailAsync(message);
    }
    
    private MimeMessage CreateEmailMessage(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_setting.FromName, _setting.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };
        return message;
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        var accessToken = await _authService.GetAccessTokenAsync();
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_setting.SmtpHost, _setting.SmtpPort, SecureSocketOptions.StartTls);
        var oauth2 = new SaslMechanismOAuth2(_setting.FromEmail, accessToken);
        await smtp.AuthenticateAsync(oauth2);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}