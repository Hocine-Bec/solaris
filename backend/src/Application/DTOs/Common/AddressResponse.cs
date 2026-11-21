using Domain.Entities;

namespace Application.DTOs.Common;

/// <summary>A US property's address, as the staff app shows it: the street, then the city, state and ZIP code.</summary>
public sealed record AddressResponse(string Street, string City, string State, string ZipCode)
{
    public static AddressResponse From(Address address) =>
        new(address.Street, address.City, address.State, address.ZipCode);
}
