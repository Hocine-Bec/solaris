namespace Application.Interfaces.Services;

public interface IEmailQueueHandler
{
    Task SendLeadWelcomeEmailAsync(int leadId, string email, string firstName, string address);
    Task SendSalesNotificationEmailAsync(int leadId, string fullName, string email, string phoneNumber, string address);
    void EnqueueLeadEmails(int leadId, string email, string firstName, string fullName, string phoneNumber, string address);
}