namespace Application.DTOs.InstallationStatusHistory;

public class PhaseAnalyticsResponse
{
    public string PhaseName { get; set; } = string.Empty;
    public double AverageDays { get; set; }
    public int MinDays { get; set; }
    public int MaxDays { get; set; }
    public int TotalInstallations { get; set; }
}