namespace Application.Common.Models;

/// <summary>
/// One email, ready to send. <paramref name="NotificationKey"/> names the notification it delivers, such as
/// <c>LeadWelcome:123</c>; the measurement harness counts deliveries by it (docs/measurements.md §4).
/// </summary>
public sealed record EmailMessage(string NotificationKey, string ToAddress, string ToName, string Subject,
    string HtmlBody);
