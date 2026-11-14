using System.Diagnostics;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// The Hangfire jobs that send a new lead's emails. Each reads the lead by its id when it runs; a lead that no longer
/// exists is logged and skipped, as retrying can't bring it back.
/// </summary>
public class EmailQueueHandler(ILeadRepo leads, IEmailService service, ILogger<EmailQueueHandler> logger)
    : IEmailQueueHandler
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public Task SendLeadWelcomeEmailAsync(int leadId) =>
        SendAsync(leadId, "lead welcome email", service.SendLeadWelcomeAsync);

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public Task SendSalesNotificationEmailAsync(int leadId) =>
        SendAsync(leadId, "new lead alert", service.SendNewLeadAlertAsync);

    private async Task SendAsync(int leadId, string email, Func<Lead, Task> send)
    {
        var stopwatch = Stopwatch.StartNew();
        var lead = await leads.GetByIdAsync(leadId);
        if (lead is null)
        {
            logger.LogWarning("Skipped the {Email} for lead {LeadId}: the lead doesn't exist", email, leadId);
            return;
        }

        try
        {
            await send(lead);
            logger.LogInformation("Sent the {Email} for lead {LeadId} in {ElapsedMs}ms", email, leadId,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the {Email} for lead {LeadId} after {ElapsedMs}ms", email, leadId,
                stopwatch.ElapsedMilliseconds);
            throw; // Hangfire retries it
        }
    }
}
