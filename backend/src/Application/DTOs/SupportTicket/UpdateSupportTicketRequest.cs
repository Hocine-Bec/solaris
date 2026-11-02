namespace Application.DTOs.SupportTicket;

public class UpdateSupportTicketRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? ResolutionNotes { get; set; }
}