namespace Application.Interfaces.Services;

public interface IEmailService
{
    public Task SendLeadEmailAsync(string toEmail, string toName, string propertyAddress);
    public Task SendSalesNotificationEmailAsync(int toCustomerId, string toName, string toEmail,
        string toPhone, string toAddress);
}