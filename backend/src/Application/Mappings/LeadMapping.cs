using Application.DTOs.Lead;
using Domain.Entities;
using Domain.Enums;
using Mapster;

namespace Application.Mappings;

public class LeadMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // The quote form's request: the lead and its property. The service stores the property once, by its key (ADR 5).
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
    }
}
