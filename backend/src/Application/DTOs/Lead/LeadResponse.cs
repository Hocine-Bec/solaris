namespace Application.DTOs.Lead;

public class LeadResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    // Property info
    public string PropertyType { get; set; } = string.Empty;
    public bool IsPropertyOwner { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ContactedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public string MonthlyBillRange { get; set; } = string.Empty;
    public string BestTimeToContact { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    // Address info
    public string PropertyAddress { get; set; } = string.Empty;
    
    // Customer info
    public int? CustomerId { get; set; }
}