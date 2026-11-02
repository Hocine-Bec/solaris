using Domain.Entities;
using Mapster;
using Application.DTOs.Address;

namespace Application.Mappings;

public class AddressMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to AddressResponse
        config.NewConfig<Address, AddressResponse>()
            .Map(dest => dest.FullAddress, src => $"{src.Street}, {src.City}, {src.State} {src.ZipCode}, {src.Country}");

        // CreateAddressRequest to Entity
        config.NewConfig<CreateAddressRequest, Address>()
            .Ignore(dest => dest.Id);

        // UpdateAddressRequest to Entity
        config.NewConfig<UpdateAddressRequest, Address>()
            .IgnoreNullValues(true)
            .Ignore(dest => dest.Id);
    }
}