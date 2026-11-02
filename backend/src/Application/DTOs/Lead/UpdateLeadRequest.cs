namespace Application.DTOs.Lead;

/// <summary>
/// DTO for updating lead status and notes
/// </summary>
public class UpdateLeadStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public DateTime? ContactedAt { get; set; }
    public string? Notes { get; set; }
}