namespace Domain.Entities;

/// <summary>
/// Junction table tracking technician assignments to installations
/// </summary>
public class InstallationTechnician
{
    public int Id { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Role { get; set; } // "Surveyor", "Installer", "Inspector"
    public string? Notes { get; set; }
        
    public int InstallationId { get; set; }
    public required Installation Installation { get; set; }
        
    public int TechnicianId { get; set; }
    public required User Technician { get; set; }
}