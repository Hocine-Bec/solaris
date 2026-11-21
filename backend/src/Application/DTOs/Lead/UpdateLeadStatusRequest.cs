namespace Application.DTOs.Lead;

/// <summary>
/// A lead's next status: Contacted, Disqualified or Lost (BR-L2). Converted comes only from the conversion; the time
/// is the server's.
/// </summary>
public sealed class UpdateLeadStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
