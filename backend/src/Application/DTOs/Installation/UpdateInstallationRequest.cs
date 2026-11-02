namespace Application.DTOs.Installation;

public class UpdateInstallationRequest
{
    public string? ProjectName { get; set; }
    public string? Status { get; set; }
    public DateTime? CompletionDate { get; set; }
    public decimal? SystemSizeKw { get; set; }
    public int? PanelCount { get; set; }
    public string? InverterType { get; set; }
    public string? Notes { get; set; }
}