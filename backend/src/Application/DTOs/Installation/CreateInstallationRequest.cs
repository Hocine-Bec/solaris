namespace Application.DTOs.Installation;

public class CreateInstallationRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public decimal SystemSizeKw { get; set; }
    public int PanelCount { get; set; }
    public string? InverterType { get; set; }
    public string? Notes { get; set; }
    public int CustomerId { get; set; }
    public int InstallationAddressId { get; set; }
}