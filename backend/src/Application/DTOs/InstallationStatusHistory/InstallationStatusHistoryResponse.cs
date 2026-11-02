namespace Application.DTOs.InstallationStatusHistory;

public class InstallationStatusHistoryResponse
{
    public int Id { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
    public string ChangedByUser { get; set; } = string.Empty;
    public int DaysInPreviousStatus { get; set; }
}