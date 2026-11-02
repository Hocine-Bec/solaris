using Application.DTOs.Lead;
using Domain.Entities;
using Domain.Enums;
using Mapster;

namespace Application.Mappings;

public class LeadMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateLeadRequest, Lead>()
            .Map(dest => dest.Address, src => new Address
            {
                Street = src.PropertyStreet,
                City = src.PropertyCity,
                State = src.PropertyState,
                ZipCode = src.PropertyZipCode,
                Country = src.PropertyCountry,
                Latitude = src.PropertyLatitude,
                Longitude = src.PropertyLongitude
            })
            .Map(dest => dest.PropertyType, src => Enum.Parse<PropertyType>(src.PropertyType, true));

        config.NewConfig<Lead, LeadResponse>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
                .Map(dest => dest.PropertyAddress,
                src => $"{src.Address.Street}, {src.Address.State} {src.Address.ZipCode}, {src.Address.City}, {src.Address.Country}")
            .Map(dest => dest.PropertyType, src => src.PropertyType.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString());

        config.NewConfig<UpdateLeadStatusRequest, Lead>()
            .Map(dest => dest.Status, src => Enum.Parse<LeadStatus>(src.Status, true))
            .IgnoreNullValues(true);

        config.NewConfig<Lead, Customer>()
            .Map(dest => dest.Id, src => 0)
            .Map(dest => dest.Status, src => CustomerStatus.Prospect)
            .Map(dest => dest.RegistrationDate, src => DateTime.UtcNow)
            .Map(dest => dest.ContactAddressId, src => src.AddressId);
    }
}