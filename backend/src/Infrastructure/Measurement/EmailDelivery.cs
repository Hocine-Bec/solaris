namespace Infrastructure.Measurement;

/// <summary>
/// One email the mail server accepted, in <c>email_delivery_log</c>: the measurement harness counts deliveries and
/// duplicates with it (docs/measurements.md §4). Only <see cref="DeliveryLoggingEmailSender"/> writes it.
/// </summary>
public sealed class EmailDelivery
{
    public long Id { get; set; }

    /// <summary>The notification this email delivers, such as <c>LeadWelcome:123</c>. Not unique: a re-send is a second row.</summary>
    public string NotificationKey { get; set; } = string.Empty;

    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
