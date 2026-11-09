using Application.Interfaces.Services;

namespace Infrastructure.Measurement;

/// <summary>
/// The P1 "before" (docs/measurements.md §6), allowed in Development only (<c>Notifications:Mode=Synchronous</c>): the
/// request sends the lead's two emails itself, one after the other, before it answers. A failed send fails the
/// request, after the lead is saved.
/// </summary>
public sealed class SynchronousLeadNotifier(IEmailService emails) : ILeadNotifier
{
    public async Task NotifyNewLeadAsync(int leadId, string email, string firstName, string fullName,
        string phoneNumber, string address)
    {
        await emails.SendLeadEmailAsync(leadId, email, firstName, address);
        await emails.SendSalesNotificationEmailAsync(leadId, fullName, email, phoneNumber, address);
    }
}
