namespace Infrastructure.Settings;

/// <summary>The <c>Notifications</c> section: how a new lead's emails leave the request.</summary>
public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    /// <summary>The startup validation keeps Synchronous in Development (<c>DependencyInjection</c>).</summary>
    public NotificationsMode Mode { get; set; } = NotificationsMode.Hangfire;
}

public enum NotificationsMode
{
    /// <summary>The request enqueues two Hangfire jobs, which send the emails in the background.</summary>
    Hangfire,

    /// <summary>Measurements only: the request sends both emails itself before it answers.</summary>
    Synchronous
}
