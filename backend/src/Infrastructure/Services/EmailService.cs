using Application.Common.Models;
using Application.Interfaces.Services;
using Infrastructure.Services.Extensions;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Renders the two new-lead emails and sends them through <see cref="IEmailSender"/>. Each carries the key of the
/// notification it delivers: <c>LeadWelcome:{leadId}</c> and <c>SalesNewLeadAlert:{leadId}</c>.
/// </summary>
public class EmailService(IEmailSender sender, IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _setting = options.Value;

    public async Task SendLeadEmailAsync(int leadId, string toEmail, string toName, string propertyAddress)
    {
        const string subject = "Your Solar Quote Request - We're on it! ☀️";
        var htmlBody = EmailTemplate.GetLeadWelcomeTemplate(toName, propertyAddress);
        await sender.SendAsync(new EmailMessage($"LeadWelcome:{leadId}", toEmail, toName, subject, htmlBody));
    }

    public async Task SendSalesNotificationEmailAsync(int leadId, string toName, string toEmail, string toPhone,
        string toAddress)
    {
        var subject = $"🔔 New Lead: {toName}";
        var htmlBody = EmailTemplate.GetSalesNotificationTemplate(leadId, toName, toEmail, toPhone, toAddress);
        await sender.SendAsync(new EmailMessage($"SalesNewLeadAlert:{leadId}", _setting.FromAddress,
            _setting.FromName, subject, htmlBody));
    }
}
