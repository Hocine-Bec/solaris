using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Customer support ticket system
/// </summary>
public class SupportTicket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
        
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
        
    public string? ResolutionNotes { get; set; }
        
    // Relationships
    public int CustomerId { get; set; }
    public required Customer Customer { get; set; }
        
    public int? AssignedToUserId { get; set; }
    public User? AssignedTo { get; set; }
        
    public int? InstallationId { get; set; }
    public Installation? Installation { get; set; }
        
    // Navigation properties
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}