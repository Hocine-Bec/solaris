using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a solar installation project
/// </summary>
public class Installation
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public InstallationStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
        
    // System specifications
    public decimal SystemSizeKw { get; set; }
    public int PanelCount { get; set; }
    public string? InverterType { get; set; }
    public string? Notes { get; set; }
        
    // Relationships
    public int CustomerId { get; set; }
    public required Customer Customer { get; set; }
        
    public int InstallationAddressId { get; set; }
    public required Address InstallationAddress { get; set; }
        
    // Navigation properties
    public ICollection<InstallationStatusHistory> StatusHistory { get; set; } = new List<InstallationStatusHistory>();
    public ICollection<InstallationTechnician> AssignedTechnicians { get; set; } = new List<InstallationTechnician>();
    public ICollection<EnergyProduction> EnergyProductions { get; set; } = new List<EnergyProduction>();
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}