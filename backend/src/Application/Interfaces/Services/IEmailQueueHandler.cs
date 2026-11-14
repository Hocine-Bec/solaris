namespace Application.Interfaces.Services;

/// <summary>
/// The background jobs that send a new lead's emails. Each job carries the lead's id only and reads the lead when it
/// runs, so no personal details are copied into the job store.
/// </summary>
public interface IEmailQueueHandler
{
    Task SendLeadWelcomeEmailAsync(int leadId);
    Task SendSalesNotificationEmailAsync(int leadId);
}
