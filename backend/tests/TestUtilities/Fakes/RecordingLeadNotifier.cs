using Application.Interfaces.Services;

namespace TestUtilities.Fakes;

/// <summary>Records the new leads a service asks to notify, without Hangfire or a mail server.</summary>
public sealed class RecordingLeadNotifier : ILeadNotifier
{
    public List<NotifiedLead> Notified { get; } = [];

    public Task NotifyNewLeadAsync(int leadId, string email, string firstName, string fullName, string phoneNumber,
        string address)
    {
        Notified.Add(new NotifiedLead(leadId, email));
        return Task.CompletedTask;
    }
}

public sealed record NotifiedLead(int LeadId, string Email);
