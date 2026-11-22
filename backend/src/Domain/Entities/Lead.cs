using Domain.Enums;

namespace Domain.Entities;

public class Lead
{
    /// <summary>
    /// The moves BR-L2 allows: New → Contacted, Disqualified or Lost; Contacted → Converted, Disqualified or Lost.
    /// Converted, Disqualified and Lost are final. Converting also makes a customer and an installation, so the API
    /// reaches Converted only through the conversion.
    /// </summary>
    private static readonly IReadOnlyDictionary<LeadStatus, LeadStatus[]> Moves = new Dictionary<LeadStatus, LeadStatus[]>
    {
        [LeadStatus.New] = [LeadStatus.Contacted, LeadStatus.Disqualified, LeadStatus.Lost],
        [LeadStatus.Contacted] = [LeadStatus.Converted, LeadStatus.Disqualified, LeadStatus.Lost],
    };

    public int Id { get; set; }
    // Contact Info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The email as it is compared (<see cref="Common.Emails.Normalize"/>): only one open lead may have it, which a
    /// partial unique index enforces (BR-L1).
    /// </summary>
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    // Property Info
    public int AddressId { get; set; }
    public Address Address { get; set; } =  null!;
    public PropertyType PropertyType { get; set; }
    public bool IsPropertyOwner { get; set; }
    // Tracking: each time is set by the move that reaches it (BR-L2)
    public LeadStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ContactedAt { get; set; }
    /// <summary>When the lead became Converted, Disqualified or Lost.</summary>
    public DateTime? ClosedAt { get; set; }
    // Qualification
    public string MonthlyBillRange { get; set; } = string.Empty;
    public string BestTimeToContact { get; set; } = string.Empty;
    /// <summary>The visitor's note, as sent with the quote form; never replaced.</summary>
    public string? Notes { get; set; }
    // Relationships
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>
    /// PostgreSQL's <c>xmin</c>, the row's version: a save made on an older version is refused, so two moves made at
    /// the same moment (a conversion and a disqualification, two conversions) can't both succeed (BR-L3).
    /// </summary>
    public uint Version { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Open while New or Contacted: only one open lead per email (BR-L1), and only an open lead is edited.</summary>
    public bool IsOpen => Status is LeadStatus.New or LeadStatus.Contacted;

    /// <summary>What the lead can become now (BR-L2); nothing once it is closed.</summary>
    public IReadOnlyList<LeadStatus> NextStatuses => Moves.GetValueOrDefault(Status, []);

    /// <summary>
    /// Moves the lead to <paramref name="status"/> if BR-L2 allows it, recording when: the contact time for Contacted,
    /// the closing time for Converted, Disqualified or Lost. Returns false, changing nothing, if it doesn't.
    /// </summary>
    public bool TryMoveTo(LeadStatus status, DateTime now)
    {
        if (!NextStatuses.Contains(status))
            return false;

        Status = status;
        if (status == LeadStatus.Contacted)
            ContactedAt = now;
        else
            ClosedAt = now;
        return true;
    }
}
