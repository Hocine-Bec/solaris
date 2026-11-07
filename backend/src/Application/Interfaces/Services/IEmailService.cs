namespace Application.Interfaces.Services;

/// <summary>Renders the lead emails and sends them through <see cref="IEmailSender"/>.</summary>
public interface IEmailService
{
    public Task SendLeadEmailAsync(int leadId, string toEmail, string toName, string propertyAddress);
    public Task SendSalesNotificationEmailAsync(int leadId, string toName, string toEmail, string toPhone,
        string toAddress);
}
