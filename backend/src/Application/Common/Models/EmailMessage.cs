namespace Application.Common.Models;

/// <summary>
/// One email, ready to send: an HTML part and a plain-text part with the same content. <paramref name="NotificationKey"/>
/// names the notification it delivers, such as <c>LeadWelcome:123</c>; the measurement harness counts deliveries by it
/// (docs/measurements.md §4).
/// </summary>
public sealed record EmailMessage(string NotificationKey, string ToAddress, string ToName, string Subject,
    string HtmlBody, string TextBody)
{
    /// <summary>Where replies go, when not to the sender.</summary>
    public EmailAddress? ReplyTo { get; init; }

    /// <summary>The images the HTML shows by their content id (<c>cid:</c>), sent inside the email.</summary>
    public IReadOnlyList<InlineImage> InlineImages { get; init; } = [];
}

public sealed record EmailAddress(string Address, string Name);

/// <summary>An image sent inside an email; the HTML shows it as <c>cid:{ContentId}</c>.</summary>
public sealed record InlineImage(string ContentId, string MediaType, byte[] Content);
