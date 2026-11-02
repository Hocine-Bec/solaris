using Application.DTOs.Address;
using Application.DTOs.Document;
using Application.DTOs.EnergyProduction;
using Application.DTOs.Equipment;
using Application.DTOs.InstallationStatusHistory;
using Application.DTOs.InstallationTechnician;

namespace Application.DTOs.Installation;

public class InstallationDetailResponse
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public decimal SystemSizeKw { get; set; }
    public int PanelCount { get; set; }
    public string? InverterType { get; set; }
    public string? Notes { get; set; }
    public CustomerSummaryResponse Customer { get; set; } = new();
    public AddressResponse InstallationAddress { get; set; } = new();
    public List<InstallationStatusHistoryResponse> StatusHistory { get; set; } = new();
    public List<TechnicianAssignmentResponse> AssignedTechnicians { get; set; } = new();
    public List<EquipmentResponse> Equipment { get; set; } = new();
    public List<DocumentResponse> Documents { get; set; } = new();
    public EnergyProductionSummaryResponse? EnergyProduction { get; set; }
}