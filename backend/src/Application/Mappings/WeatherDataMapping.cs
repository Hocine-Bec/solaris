using Application.DTOs.WeatherData;
using Domain.Entities;
using Mapster;

namespace Application.Mappings;

public class WeatherDataMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to WeatherDataResponse
        config.NewConfig<WeatherData, WeatherDataResponse>()
            .Map(dest => dest.TemperatureFahrenheit, src => (src.TemperatureCelsius * 9 / 5) + 32);

        // CreateWeatherDataRequest to Entity
        config.NewConfig<CreateWeatherDataRequest, WeatherData>()
            .Ignore(dest => dest.Id);

        // UpdateWeatherDataRequest to Entity
        config.NewConfig<UpdateWeatherDataRequest, WeatherData>()
            .IgnoreNullValues(true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Date)
            .Ignore(dest => dest.Location);
    }
}