using Application.DTOs.Common;

namespace Application.DTOs.Lead;

/// <summary>A lead in the list (FR-L3): who, which property, its status, when it came in.</summary>
public sealed record LeadSummaryResponse(
    int Id,
    string Name,
    string Email,
    AddressResponse Address,
    string PropertyType,
    string Status,
    DateTime CreatedAt)
{
    public static LeadSummaryResponse From(Domain.Entities.Lead lead) =>
        new(lead.Id, lead.FullName, lead.Email, AddressResponse.From(lead.Address), lead.PropertyType.ToString(),
            lead.Status.ToString(), lead.CreatedAt);
}
