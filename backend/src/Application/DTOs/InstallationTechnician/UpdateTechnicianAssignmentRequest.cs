namespace Application.DTOs.InstallationTechnician;

public class UpdateTechnicianAssignmentRequest
{
    public DateTime? CompletedDate { get; set; }
    public string? Role { get; set; }
    public string? Notes { get; set; }
}