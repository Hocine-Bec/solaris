using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;

namespace Infrastructure.Outbox;

/// <summary>
/// The default mode (<c>Notifications:Mode=Outbox</c>): a new lead's two notifications are added to the lead's own save,
/// so both are stored or neither is (BR-N1). The request never talks to the mail server; the
/// <see cref="OutboxDispatcher"/> sends them.
/// </summary>
public sealed class OutboxLeadNotifier(AppDbContext context, TimeProvider time) : ILeadNotifier
{
    public void OnSaving(Lead lead)
    {
        var now = time.GetUtcNow().UtcDateTime;
        context.OutboxMessages.Add(OutboxMessage.ForLead(lead, OutboxMessageType.LeadWelcome, now));
        context.OutboxMessages.Add(OutboxMessage.ForLead(lead, OutboxMessageType.SalesNewLeadAlert, now));
    }

    public Task OnSavedAsync(Lead lead) => Task.CompletedTask;
}
