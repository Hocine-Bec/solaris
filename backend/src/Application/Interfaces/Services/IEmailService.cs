namespace Application.Interfaces.Services;

public interface IEmailService
{
    // Task SendLeadCreatedEmailsAsync(int leadId, string email, string firstName, 
    //     string fullName, string phoneNumber, string fullAddress);
    public Task SendLeadEmailAsync(string toEmail, string toName, string propertyAddress);
    public Task SendSalesNotificationEmailAsync(int toCustomerId, string toName, string toEmail,
        string toPhone, string toAddress);
}