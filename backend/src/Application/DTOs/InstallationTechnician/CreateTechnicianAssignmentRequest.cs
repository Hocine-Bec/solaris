namespace Application.DTOs.InstallationTechnician;

public class CreateTechnicianAssignmentRequest
{
    public int InstallationId { get; set; }
    public int TechnicianId { get; set; }
    public string? Role { get; set; }
    public string? Notes { get; set; }
}