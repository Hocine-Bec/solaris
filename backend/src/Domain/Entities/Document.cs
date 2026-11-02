using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Generic document storage with polymorphic relationships
/// </summary>
public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; 
    public long FileSize { get; set; }
    public DocumentType Type { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
        
    // Polymorphic relationships (one must be set)
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
        
    public int? InstallationId { get; set; }
    public Installation? Installation { get; set; }
        
    public int? TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }
        
    // Who uploaded it
    public int UploadedByUserId { get; set; }
    public required User UploadedBy { get; set; }
}