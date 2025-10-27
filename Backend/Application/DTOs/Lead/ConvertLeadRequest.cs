namespace Application.DTOs.Lead;

/// <summary>
/// DTO for converting lead to customer
/// </summary>
public class ConvertLeadRequest
{
    public int LeadId { get; set; }
    public string? AdditionalNotes { get; set; }
}