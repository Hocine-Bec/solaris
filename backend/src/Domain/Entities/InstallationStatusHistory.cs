using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Tracks installation progress through workflow stages
/// </summary>
public class InstallationStatusHistory
{
    public int Id { get; set; }
    public InstallationStatus FromStatus { get; set; }
    public InstallationStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
        
    public int InstallationId { get; set; }
    public required Installation Installation { get; set; }
        
    public int ChangedByUserId { get; set; }
    public required User ChangedBy { get; set; }
}