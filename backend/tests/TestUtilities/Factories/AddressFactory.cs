using Bogus;
using Domain.Entities;

namespace TestUtilities.Factories;

public static class AddressFactory
{
    private static readonly Faker<Address> _addressFaker;

    static AddressFactory()
    {
        _addressFaker = new Faker<Address>()
            .RuleFor(a => a.Id, f => f.IndexFaker)
            .RuleFor(a => a.Street, f => f.Address.StreetAddress())
            .RuleFor(a => a.City, f => f.Address.City())
            .RuleFor(a => a.State, f => f.Address.StateAbbr())
            .RuleFor(a => a.ZipCode, f => f.Address.ZipCode("#####"))
            .RuleFor(a => a.Country, f => "US")
            .RuleFor(a => a.Latitude, f => (decimal)f.Address.Latitude())
            .RuleFor(a => a.Longitude, f => (decimal)f.Address.Longitude())
            .RuleFor(a => a.UniqueAddressHash, (f, a) => 
                Address.BuildHash(a.Street, a.City, a.State, a.ZipCode, a.Country));
    }

    public static Address CreateFakeAddress(
        int? id = null,
        string? street = null,
        string? city = null,
        string? state = null,
        string? zipCode = null,
        string? country = null)
    {
        var address = _addressFaker.Generate();
        
        if (id.HasValue) address.Id = id.Value;
        if (!string.IsNullOrEmpty(street)) address.Street = street;
        if (!string.IsNullOrEmpty(city)) address.City = city;
        if (!string.IsNullOrEmpty(state)) address.State = state;
        if (!string.IsNullOrEmpty(zipCode)) address.ZipCode = zipCode;
        if (!string.IsNullOrEmpty(country)) address.Country = country;
        
        address.UniqueAddressHash = Address.BuildHash(
            address.Street, 
            address.City, 
            address.State, 
            address.ZipCode, 
            address.Country);
            
        return address;
    }

    public static List<Address> CreateFakeAddresses(int count = 3)
    {
        return _addressFaker.Generate(count);
    }
}