using Application.Interfaces.Services;
using Domain.Entities;

namespace Infrastructure.Measurement;

/// <summary>
/// The P1 "before" (docs/measurements.md §6), allowed in Development only (<c>Notifications:Mode=Synchronous</c>): the
/// request sends the lead's two emails itself, one after the other, before it answers. A failed send fails the
/// request, after the lead is saved.
/// </summary>
public sealed class SynchronousLeadNotifier(IEmailService emails) : ILeadNotifier
{
    public async Task NotifyNewLeadAsync(Lead lead)
    {
        await emails.SendLeadWelcomeAsync(lead);
        await emails.SendNewLeadAlertAsync(lead);
    }
}
