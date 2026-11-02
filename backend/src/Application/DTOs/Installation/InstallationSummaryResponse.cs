namespace Application.DTOs.Installation;

public class InstallationSummaryResponse
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SystemSizeKw { get; set; }
    public DateTime StartDate { get; set; }
    public string InstallationAddress { get; set; } = string.Empty;
}