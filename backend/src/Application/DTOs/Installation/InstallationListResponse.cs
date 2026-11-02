namespace Application.DTOs.Installation;

public class InstallationListResponse
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal SystemSizeKw { get; set; }
    public DateTime StartDate { get; set; }
    public int DaysInCurrentPhase { get; set; }
}