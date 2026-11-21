using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A technician's part on an installation (FR-I5, FR-I6): their role, who assigned them and when, and, once done, who
/// marked it complete (they, or an Admin for them) and when.
/// </summary>
public class InstallationAssignment
{
    public int Id { get; set; }
    public int InstallationId { get; set; }
    public Installation Installation { get; set; } = null!;

    public int TechnicianId { get; set; }
    public User Technician { get; set; } = null!;

    public AssignmentRole Role { get; set; }

    public DateTime AssignedAt { get; set; }
    public int AssignedByUserId { get; set; }
    public User? AssignedBy { get; set; }

    public DateTime? CompletedAt { get; set; }
    public int? CompletedByUserId { get; set; }
    public User? CompletedBy { get; set; }

    public bool IsCompleted => CompletedAt is not null;

    /// <summary>The stage at which this part is done, and completed: a survey at Survey, and so on (point 04).</summary>
    public InstallationStatus Stage => StageOf(Role);

    public static InstallationStatus StageOf(AssignmentRole role) => role switch
    {
        AssignmentRole.Surveyor => InstallationStatus.Survey,
        AssignmentRole.Installer => InstallationStatus.Installation,
        _ => InstallationStatus.Inspection
    };
}
