namespace Application.DTOs.Installation;

public class InstallationResponse
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
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string InstallationAddress { get; set; } = string.Empty;
    public int DaysInCurrentPhase { get; set; }
}