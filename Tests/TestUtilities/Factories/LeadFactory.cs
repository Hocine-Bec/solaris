using Bogus;
using Domain.Entities;
using Domain.Enums;

namespace TestUtilities.Factories;

public static class LeadFactory
{
    private static readonly Faker<Lead> _leadFaker;

    static LeadFactory()
    {
        _leadFaker = new Faker<Lead>()
            .RuleFor(l => l.Id, f => f.IndexFaker)
            .RuleFor(l => l.FirstName, f => f.Name.FirstName())
            .RuleFor(l => l.LastName, f => f.Name.LastName())
            .RuleFor(l => l.Email, f => f.Internet.Email())
            .RuleFor(l => l.PhoneNumber, f => f.Phone.PhoneNumber("###-###-####"))
            .RuleFor(l => l.AddressId, f => f.Random.Int(1, 1000))
            .RuleFor(l => l.PropertyType, f => f.PickRandom<PropertyType>())
            .RuleFor(l => l.IsPropertyOwner, f => f.Random.Bool())
            .RuleFor(l => l.Status, f => f.PickRandom<LeadStatus>())
            .RuleFor(l => l.CreatedAt, f => f.Date.Past(1))
            .RuleFor(l => l.ContactedAt, f => f.Random.Bool() ? f.Date.Recent(30) : null)
            .RuleFor(l => l.ConvertedAt, f => null)
            .RuleFor(l => l.MonthlyBillRange, f => f.PickRandom("$50-$100", "$100-$200", "$200-$300", "$300+"))
            .RuleFor(l => l.BestTimeToContact, f => f.PickRandom("Morning", "Afternoon", "Evening"))
            .RuleFor(l => l.Notes, f => f.Random.Bool() ? f.Lorem.Sentence() : null)
            .RuleFor(l => l.CustomerId, f => null);
    }

    public static Lead CreateFakeLead(
        int? id = null,
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? phoneNumber = null,
        int? addressId = null,
        PropertyType? propertyType = null,
        bool? isPropertyOwner = null,
        LeadStatus? status = null,
        DateTime? createdAt = null,
        DateTime? contactedAt = null,
        DateTime? convertedAt = null,
        string? monthlyBillRange = null,
        string? bestTimeToContact = null,
        string? notes = null,
        int? customerId = null,
        Address? address = null)
    {
        var lead = _leadFaker.Generate();
        
        if (id.HasValue) lead.Id = id.Value;
        if (!string.IsNullOrEmpty(firstName)) lead.FirstName = firstName;
        if (!string.IsNullOrEmpty(lastName)) lead.LastName = lastName;
        if (!string.IsNullOrEmpty(email)) lead.Email = email;
        if (!string.IsNullOrEmpty(phoneNumber)) lead.PhoneNumber = phoneNumber;
        if (addressId.HasValue) lead.AddressId = addressId.Value;
        if (propertyType.HasValue) lead.PropertyType = propertyType.Value;
        if (isPropertyOwner.HasValue) lead.IsPropertyOwner = isPropertyOwner.Value;
        if (status.HasValue) lead.Status = status.Value;
        if (createdAt.HasValue) lead.CreatedAt = createdAt.Value;
        if (contactedAt.HasValue) lead.ContactedAt = contactedAt.Value;
        if (convertedAt.HasValue) lead.ConvertedAt = convertedAt.Value;
        if (!string.IsNullOrEmpty(monthlyBillRange)) lead.MonthlyBillRange = monthlyBillRange;
        if (!string.IsNullOrEmpty(bestTimeToContact)) lead.BestTimeToContact = bestTimeToContact;
        if (notes != null) lead.Notes = notes;
        if (customerId.HasValue) lead.CustomerId = customerId.Value;
        if (address != null) lead.Address = address;
            
        return lead;
    }

    public static List<Lead> CreateFakeLeads(int count = 3)
    {
        return _leadFaker.Generate(count);
    }
    
    public static Lead CreateFakeLeadWithAddress(
        int? id = null,
        LeadStatus? status = null,
        Address? address = null)
    {
        var lead = CreateFakeLead(id: id, status: status);
        
        if (address != null)
        {
            lead.Address = address;
            lead.AddressId = address.Id;
        }
        else
        {
            lead.Address = AddressFactory.CreateFakeAddress();
            lead.AddressId = lead.Address.Id;
        }
        
        return lead;
    }
}
