using Application.Interfaces.Services;

namespace TestUtilities.Fakes;

/// <summary>Records the lead emails a service asks for, without Hangfire or a mail server.</summary>
public sealed class RecordingEmailQueueHandler : IEmailQueueHandler
{
    public List<EnqueuedLeadEmails> Enqueued { get; } = [];

    public void EnqueueLeadEmails(int leadId, string email, string firstName, string fullName, string phoneNumber,
        string address) =>
        Enqueued.Add(new EnqueuedLeadEmails(leadId, email));

    public Task SendLeadWelcomeEmailAsync(int leadId, string email, string firstName, string address) =>
        throw new NotSupportedException("Only enqueuing is recorded.");

    public Task SendSalesNotificationEmailAsync(int leadId, string fullName, string email, string phoneNumber,
        string address) =>
        throw new NotSupportedException("Only enqueuing is recorded.");
}

public sealed record EnqueuedLeadEmails(int LeadId, string Email);
