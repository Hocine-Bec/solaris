using System.Diagnostics;
using Application.Interfaces.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EmailQueueHandler(IEmailService service, ILogger<EmailQueueHandler> logger) : IEmailQueueHandler
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task SendLeadWelcomeEmailAsync(int leadId, string email, string firstName, string address)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await service.SendLeadEmailAsync(email, firstName, address);
            logger.LogInformation(
                "Lead welcome email sent successfully for lead {LeadId} to {Email} in {ElapsedMs}ms",
                leadId, email, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Failed to send lead welcome email for lead {LeadId} to {Email} after {ElapsedMs}ms",
                leadId, email, stopwatch.ElapsedMilliseconds);
            throw; // Let Hangfire handle retry
        }
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task SendSalesNotificationEmailAsync(int leadId, string fullName, string email,
        string phoneNumber, string address)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await service.SendSalesNotificationEmailAsync(leadId, fullName, email, phoneNumber, address);
            logger.LogInformation(
                "Sales notification email sent successfully for lead {LeadId} in {ElapsedMs}ms",
                leadId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Failed to send sales notification email for lead {LeadId} after {ElapsedMs}ms",
                leadId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
    
    public void EnqueueLeadEmails(int leadId, string email, string firstName, string fullName,
        string phoneNumber, string address)
    {
        _ = Task.Run(() =>
        {
            try
            {
                BackgroundJob.Enqueue<IEmailQueueHandler>(x => 
                    x.SendLeadWelcomeEmailAsync(leadId, email, firstName, address));
            
                BackgroundJob.Enqueue<IEmailQueueHandler>(x => 
                    x.SendSalesNotificationEmailAsync(leadId, fullName, email, phoneNumber, address));
            
                logger.LogInformation("Successfully enqueued email jobs for lead {LeadId}", leadId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue email jobs for lead {LeadId}", leadId);
                // Jobs won't be sent, but API request succeeds
            }
        });
    
        logger.LogInformation("Queueing email jobs for lead {LeadId}", leadId);
    }
}