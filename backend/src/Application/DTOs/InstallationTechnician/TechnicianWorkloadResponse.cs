using Application.DTOs.Installation;

namespace Application.DTOs.InstallationTechnician;

public class TechnicianWorkloadResponse
{
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public int ActiveAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public List<InstallationSummaryResponse> CurrentProjects { get; set; } = new();
}