using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A notification waiting to be sent (BR-N1, BR-N2): saved in the same transaction as the event that causes it, then
/// sent by the outbox dispatcher, retried on a growing schedule, and marked Failed once its 24 hours have passed. The
/// Admin can retry a Failed one (FR-N2), which gives it another 24 hours. It holds the lead's id only: the email is
/// rendered from the lead when it is sent, so no personal details are copied here.
/// </summary>
public class OutboxMessage
{
    /// <summary>How long after each failed attempt the next one comes: the first failure waits 10 s, the eleventh and later 8 h.</summary>
    public static readonly IReadOnlyList<TimeSpan> RetryDelays =
    [
        TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1),
        TimeSpan.FromHours(2), TimeSpan.FromHours(4), TimeSpan.FromHours(8)
    ];

    /// <summary>
    /// How long a message is tried, from when it is saved or retried: the last attempt is at the end of it, and if that
    /// fails, it is Failed.
    /// </summary>
    public static readonly TimeSpan DeliveryWindow = TimeSpan.FromHours(24);

    public const int MaxErrorLength = 2000;

    /// <summary>Time-ordered (version 7), made here, and sent as the email's Message-ID.</summary>
    public Guid Id { get; private set; }

    public OutboxMessageType Type { get; private set; }

    /// <summary>The lead a lead's email is about; null for an installation's email. Exactly one of the two is set.</summary>
    public int? LeadId { get; private set; }
    public Lead? Lead { get; private set; }

    /// <summary>The installation a switch-on email is about; null for a lead's email.</summary>
    public int? InstallationId { get; private set; }
    public Installation? Installation { get; private set; }

    public OutboxMessageStatus Status { get; private set; }

    /// <summary>How many times sending was tried, the successful one included.</summary>
    public int Attempts { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>The end of its 24 hours: no attempt is scheduled after it, and a failure from then on marks it Failed.</summary>
    public DateTime GiveUpAt { get; private set; }

    /// <summary>When it is due; kept as it was once it is Sent or Failed.</summary>
    public DateTime NextAttemptAt { get; private set; }

    public DateTime? LastAttemptAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    /// <summary>Why the last attempt failed.</summary>
    public string? LastError { get; private set; }

    /// <summary>When an Admin last retried it after it Failed, and who (null if that account no longer exists).</summary>
    public DateTime? RetriedAt { get; private set; }
    public int? RetriedByUserId { get; private set; }
    public User? RetriedBy { get; private set; }

    /// <summary>The notification it delivers, as the measurement harness counts them: <c>LeadWelcome:123</c>.</summary>
    public string NotificationKey => $"{Type}:{LeadId ?? InstallationId}";

    private OutboxMessage() { }

    /// <summary>A notification about a lead, due now; saved with the lead, whose id it gets from it.</summary>
    public static OutboxMessage ForLead(Lead lead, OutboxMessageType type, DateTime now) => new()
    {
        Id = Guid.CreateVersion7(new DateTimeOffset(now, TimeSpan.Zero)),
        Type = type,
        Lead = lead,
        LeadId = lead.Id,
        Status = OutboxMessageStatus.Pending,
        CreatedAt = now,
        GiveUpAt = now + DeliveryWindow,
        NextAttemptAt = now
    };

    /// <summary>A notification about an installation (its switch-on email), due now; saved with the move to Active.</summary>
    public static OutboxMessage ForInstallation(Installation installation, OutboxMessageType type, DateTime now) => new()
    {
        Id = Guid.CreateVersion7(new DateTimeOffset(now, TimeSpan.Zero)),
        Type = type,
        Installation = installation,
        InstallationId = installation.Id,
        Status = OutboxMessageStatus.Pending,
        CreatedAt = now,
        GiveUpAt = now + DeliveryWindow,
        NextAttemptAt = now
    };

    /// <summary>The mail server accepted it.</summary>
    public void MarkSent(DateTime now)
    {
        EnsurePending();
        Attempts++;
        LastAttemptAt = now;
        SentAt = now;
        Status = OutboxMessageStatus.Sent;
    }

    /// <summary>
    /// An attempt failed (BR-N2): the next one comes after the schedule's delay for this many attempts, but never after
    /// the end of the 24 hours, so the last attempt is at that moment; a failure from then on marks it Failed.
    /// </summary>
    public void RecordFailure(string error, DateTime now)
    {
        EnsurePending();
        Attempts++;
        LastAttemptAt = now;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;

        if (now >= GiveUpAt)
        {
            Status = OutboxMessageStatus.Failed;
            return;
        }

        var next = now + RetryDelays[Math.Min(Attempts, RetryDelays.Count) - 1];
        NextAttemptAt = next < GiveUpAt ? next : GiveUpAt;
    }

    /// <summary>Not tried, but not due before <paramref name="until"/>: it waits with a message that just failed.</summary>
    public void Postpone(DateTime until)
    {
        EnsurePending();
        if (until > NextAttemptAt)
            NextAttemptAt = until;
    }

    private void EnsurePending()
    {
        if (Status != OutboxMessageStatus.Pending)
            throw new InvalidOperationException($"Outbox message {Id} is {Status}, not Pending.");
    }
}
