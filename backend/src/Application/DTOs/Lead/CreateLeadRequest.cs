namespace Application.DTOs.Lead;

public class CreateLeadRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
        
    // Property Info (from Geoapify)
    public string PropertyStreet { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string PropertyState { get; set; } = string.Empty;
    public string PropertyZipCode { get; set; } = string.Empty;
    public string PropertyCountry { get; set; } = string.Empty;
    public decimal PropertyLatitude { get; set; } 
    public decimal PropertyLongitude { get; set; } 
    public string PropertyType { get; set; } = string.Empty;
    public bool IsPropertyOwner { get; set; }
        
    // Qualification
    public string MonthlyBillRange { get; set; } = string.Empty;
    public string BestTimeToContact { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
