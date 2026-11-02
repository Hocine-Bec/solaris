namespace Application.DTOs.Document;

public class CreateDocumentRequest
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public int? InstallationId { get; set; }
    public int? TicketId { get; set; }
    public int UploadedByUserId { get; set; }
}