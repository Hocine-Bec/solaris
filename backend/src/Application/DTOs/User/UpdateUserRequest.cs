namespace Application.DTOs.User;

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? IsActive { get; set; }
    public string? Specialization { get; set; }
    public string? LicenseNumber { get; set; }
}