using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>A homeowner who went ahead: created by converting a lead, then reused by their later conversions (BR-L3).</summary>
public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>The email as it was entered.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The email as it is compared (<see cref="Emails.Normalize"/>): one customer per email.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Follows its installations (BR-C1); nobody sets it by hand.</summary>
    public CustomerStatus Status { get; set; }

    /// <summary>When the first conversion created it.</summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>Where they live: the property of the lead that created them.</summary>
    public int? ContactAddressId { get; set; }
    public Address? ContactAddress { get; set; }

    public ICollection<Installation> Installations { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>A new customer from a converted lead: its contact details, Prospect, living at the lead's property.</summary>
    public static Customer FromLead(Lead lead, DateTime now) => new()
    {
        FirstName = lead.FirstName,
        LastName = lead.LastName,
        Email = lead.Email,
        NormalizedEmail = Emails.Normalize(lead.Email),
        PhoneNumber = lead.PhoneNumber,
        Status = CustomerStatus.Prospect,
        RegistrationDate = now,
        ContactAddressId = lead.AddressId
    };
}
