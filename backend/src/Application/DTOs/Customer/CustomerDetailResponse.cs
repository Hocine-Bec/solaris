using Application.DTOs.Document;
using Application.DTOs.Installation;
using Application.DTOs.SupportTicket;

namespace Application.DTOs.Customer;

public class CustomerDetailResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public string? ContactAddress { get; set; }
    public List<InstallationSummaryResponse> Installations { get; set; } = new();
    public List<DocumentResponse> Documents { get; set; } = new();
    public List<SupportTicketSummaryResponse> RecentTickets { get; set; } = new();
}