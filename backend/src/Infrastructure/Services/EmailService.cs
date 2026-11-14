using Application.Common.Models;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Services.Email;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Renders a new lead's two emails (<see cref="LeadEmails"/>) and sends them through <see cref="IEmailSender"/>. Each
/// carries the key of the notification it delivers: <c>LeadWelcome:{leadId}</c> and <c>SalesNewLeadAlert:{leadId}</c>.
/// </summary>
public class EmailService(IEmailSender sender, IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _settings = options.Value;

    public async Task SendLeadWelcomeAsync(Lead lead)
    {
        var email = LeadEmails.Welcome(lead, _settings.SiteUrl);
        await sender.SendAsync(new EmailMessage($"LeadWelcome:{lead.Id}", lead.Email, FullName(lead), email.Subject,
            email.Html, email.Text) { InlineImages = [Wordmark.Image] });
    }

    public async Task SendNewLeadAlertAsync(Lead lead)
    {
        // To the office's own address; a reply goes to the person
        var email = LeadEmails.NewLeadAlert(lead);
        await sender.SendAsync(new EmailMessage($"SalesNewLeadAlert:{lead.Id}", _settings.FromAddress,
            _settings.FromName, email.Subject, email.Html, email.Text)
        {
            ReplyTo = new EmailAddress(lead.Email, FullName(lead)),
            InlineImages = [Wordmark.Image]
        });
    }

    private static string FullName(Lead lead) => $"{lead.FirstName} {lead.LastName}";
}
