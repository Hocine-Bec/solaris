namespace Application.DTOs.SupportTicket;

public class CreateSupportTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? InstallationId { get; set; }
}