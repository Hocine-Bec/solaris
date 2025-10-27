using Domain.Enums;

namespace Domain.Entities;

public class Lead
{
    public int Id { get; set; }
    // Contact Info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    // Property Info
    public int AddressId { get; set; }
    public Address Address { get; set; } =  null!;
    public PropertyType PropertyType { get; set; }
    public bool IsPropertyOwner { get; set; }
    // Tracking
    public LeadStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ContactedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
    // Qualification
    public string MonthlyBillRange { get; set; } = string.Empty;
    public string BestTimeToContact { get; set; } = string.Empty;
    public string? Notes { get; set; }
    // Relationships
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}