using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a customer (homeowner) in the system
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
        
    // Contact address (where customer lives/receives mail)
    public int? ContactAddressId { get; set; }
    public Address? ContactAddress { get; set; }
        
    // Navigation properties
    public ICollection<Installation> Installations { get; set; } = new List<Installation>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
}