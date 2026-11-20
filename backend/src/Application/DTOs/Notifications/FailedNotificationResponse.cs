namespace Application.DTOs.Notifications;

/// <summary>
/// An email the outbox couldn't send in its 24 hours, as the Admin's list shows it (FR-N2): which email, to whom,
/// about which lead, when it was tried, why it failed, and who retried it before.
/// </summary>
public sealed record FailedNotificationResponse(
    Guid Id,
    string Type,
    RecipientResponse To,
    LeadSummary Lead,
    DateTime CreatedAt,
    DateTime? LastAttemptAt,
    int Attempts,
    string? LastError,
    DateTime? RetriedAt,
    string? RetriedBy);

public sealed record RecipientResponse(string Name, string Address);

public sealed record LeadSummary(int Id, string Name);

/// <summary>How many failed emails a Retry all sent again.</summary>
public sealed record RetriedResponse(int Retried);
