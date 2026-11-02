namespace Application.DTOs.InstallationStatusHistory;

public class CreateInstallationStatusHistoryRequest
{
    public int InstallationId { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int ChangedByUserId { get; set; }
}