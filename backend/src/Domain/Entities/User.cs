using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// System users (Admins, Technicians, Customers)
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
        
    // For technicians - additional profile info
    public string? Specialization { get; set; }
    public string? LicenseNumber { get; set; }
        
    // Navigation properties
    public ICollection<InstallationTechnician> AssignedInstallations { get; set; } = new List<InstallationTechnician>();
    public ICollection<SupportTicket> AssignedTickets { get; set; } = new List<SupportTicket>();
}