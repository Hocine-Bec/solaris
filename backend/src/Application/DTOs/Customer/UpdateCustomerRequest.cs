namespace Application.DTOs.Customer;

public class UpdateCustomerRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Status { get; set; }
    public int? ContactAddressId { get; set; }
}