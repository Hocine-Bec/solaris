using Application.Interfaces.Services;
using Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// The default mode (<c>Notifications:Mode=Hangfire</c>): enqueues one Hangfire job per email, fire-and-forget, and
/// returns at once; <see cref="EmailQueueHandler"/> sends them in the background. Each job carries the lead's id only.
/// </summary>
public class HangfireLeadNotifier(ILogger<HangfireLeadNotifier> logger) : ILeadNotifier
{
    public Task NotifyNewLeadAsync(Lead lead)
    {
        var leadId = lead.Id;
        _ = Task.Run(() =>
        {
            try
            {
                BackgroundJob.Enqueue<IEmailQueueHandler>(x => x.SendLeadWelcomeEmailAsync(leadId));

                BackgroundJob.Enqueue<IEmailQueueHandler>(x => x.SendSalesNotificationEmailAsync(leadId));

                logger.LogInformation("Successfully enqueued email jobs for lead {LeadId}", leadId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue email jobs for lead {LeadId}", leadId);
                // Jobs won't be sent, but API request succeeds
            }
        });

        logger.LogInformation("Queueing email jobs for lead {LeadId}", leadId);
        return Task.CompletedTask;
    }
}
