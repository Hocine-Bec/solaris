namespace Infrastructure.Settings;

/// <summary>The <c>Notifications</c> section: how a new lead's emails leave the request.</summary>
public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    /// <summary>The startup validation keeps Synchronous in Development (<c>DependencyInjection</c>).</summary>
    public NotificationsMode Mode { get; set; } = NotificationsMode.Outbox;
}

public enum NotificationsMode
{
    /// <summary>
    /// The request saves two outbox messages with the lead, in one transaction; the outbox dispatcher sends them in the
    /// background (BR-N1, BR-N2).
    /// </summary>
    Outbox,

    /// <summary>Measurements only: the request sends both emails itself before it answers.</summary>
    Synchronous
}
