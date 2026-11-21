namespace Application.DTOs.Notifications;

/// <summary>
/// An email the outbox couldn't send in its 24 hours, as the Admin's list shows it (FR-N2): which email, to whom,
/// about which lead or which installation (exactly one is set), when it was tried, why it failed, and who retried it
/// before.
/// </summary>
public sealed record FailedNotificationResponse(
    Guid Id,
    string Type,
    RecipientResponse To,
    LeadSummary? Lead,
    InstallationRef? Installation,
    DateTime CreatedAt,
    DateTime? LastAttemptAt,
    int Attempts,
    string? LastError,
    DateTime? RetriedAt,
    string? RetriedBy);

public sealed record RecipientResponse(string Name, string Address);

public sealed record LeadSummary(int Id, string Name);

/// <summary>The installation a switch-on email is about: its id and its street.</summary>
public sealed record InstallationRef(int Id, string Street);

/// <summary>How many failed emails a Retry all sent again.</summary>
public sealed record RetriedResponse(int Retried);
