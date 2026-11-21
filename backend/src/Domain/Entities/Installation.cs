using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One solar project for one customer at one address, from the site visit to a working system. A conversion creates
/// it at Survey, at the lead's property (BR-L3).
/// </summary>
public class Installation
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int AddressId { get; set; }
    public Address Address { get; set; } = null!;

    /// <summary>The lead whose conversion created it.</summary>
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public InstallationStatus Status { get; set; }

    /// <summary>
    /// Its IANA time zone, in which its production is counted per local day: proposed from its state at conversion
    /// (<see cref="Common.UsTimeZones"/>), then confirmed by the Admin until it is Active (BR-P4).
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>A new installation for a converted lead: at Survey, at the lead's property, in its state's time zone.</summary>
    public static Installation ForConvertedLead(Lead lead, int customerId, string timeZone, DateTime now) => new()
    {
        CustomerId = customerId,
        AddressId = lead.AddressId,
        LeadId = lead.Id,
        Status = InstallationStatus.Survey,
        TimeZone = timeZone,
        CreatedAt = now
    };
}
