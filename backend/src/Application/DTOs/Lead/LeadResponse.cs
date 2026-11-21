using Application.DTOs.Common;
using Domain.Common;

namespace Application.DTOs.Lead;

/// <summary>
/// One lead, as its page shows it (FR-L3–L5, FR-L7): the request as the visitor sent it, when each move happened, what
/// it can become now (<see cref="Next"/>, BR-L2), and its customer: the one it was converted into or, while it is open,
/// the customer that already has its email (whom a conversion would reuse). While it is open, the time zone a
/// conversion would give its installation (BR-P4), so the screen shows the rule's answer without repeating the rule.
/// </summary>
public sealed record LeadResponse(
    int Id,
    string FirstName,
    string LastName,
    string Name,
    string Email,
    string Phone,
    string BestTimeToContact,
    AddressResponse Address,
    string PropertyType,
    bool IsPropertyOwner,
    string MonthlyBillRange,
    string? Notes,
    string Status,
    DateTime CreatedAt,
    DateTime? ContactedAt,
    DateTime? ClosedAt,
    IReadOnlyList<string> Next,
    CustomerLinkResponse? Customer,
    int? InstallationId,
    TimeZoneResponse? ProposedTimeZone)
{
    public static LeadResponse From(Domain.Entities.Lead lead, Domain.Entities.Customer? customer, int? installationId) =>
        new(lead.Id, lead.FirstName, lead.LastName, lead.FullName, lead.Email, lead.PhoneNumber, lead.BestTimeToContact,
            AddressResponse.From(lead.Address), lead.PropertyType.ToString(), lead.IsPropertyOwner, lead.MonthlyBillRange,
            lead.Notes, lead.Status.ToString(), lead.CreatedAt, lead.ContactedAt, lead.ClosedAt,
            lead.NextStatuses.Select(s => s.ToString()).ToList(),
            customer is null ? null : CustomerLinkResponse.From(customer), installationId,
            lead.IsOpen && UsTimeZones.ProposalFor(lead.Address.State) is { } zone
                ? new TimeZoneResponse(zone.Id, zone.Name)
                : null);
}

/// <summary>An IANA time zone and the name the staff app shows: <c>America/Chicago</c>, Central.</summary>
public sealed record TimeZoneResponse(string Id, string Name);

/// <summary>A customer named on a lead's page: who, and since when.</summary>
public sealed record CustomerLinkResponse(int Id, string Name, DateTime Since)
{
    public static CustomerLinkResponse From(Domain.Entities.Customer customer) =>
        new(customer.Id, customer.FullName, customer.RegistrationDate);
}
