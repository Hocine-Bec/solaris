namespace Application.DTOs.InstallationTechnician;

public class TechnicianAssignmentResponse
{
    public int Id { get; set; }
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}