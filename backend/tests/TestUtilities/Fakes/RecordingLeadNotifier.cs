using Application.Interfaces.Services;
using Domain.Entities;

namespace TestUtilities.Fakes;

/// <summary>Records the new leads a service asks to notify, without Hangfire or a mail server.</summary>
public sealed class RecordingLeadNotifier : ILeadNotifier
{
    public List<NotifiedLead> Notified { get; } = [];

    public Task NotifyNewLeadAsync(Lead lead)
    {
        Notified.Add(new NotifiedLead(lead.Id, lead.Email));
        return Task.CompletedTask;
    }
}

public sealed record NotifiedLead(int LeadId, string Email);
