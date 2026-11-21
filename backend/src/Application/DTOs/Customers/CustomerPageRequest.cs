using Application.DTOs.Common;

namespace Application.DTOs.Customers;

/// <summary>A page of the customer list (FR-C1): <c>?q=raman&amp;page=1</c>, <c>q</c> being part of a name or an email.</summary>
public sealed class CustomerPageRequest : PageRequest
{
    public const int MaxSearchLength = 100;

    public string? Q { get; set; }
}
