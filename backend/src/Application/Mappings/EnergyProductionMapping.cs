using Domain.Entities;
using Domain.Enums;
using Mapster;
using Application.DTOs.EnergyProduction;

namespace Application.Mappings;

public class EnergyProductionMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to EnergyProductionResponse
        config.NewConfig<EnergyProduction, EnergyProductionResponse>()
            .Map(dest => dest.VarianceKwh, src => src.ActualProductionKwh - src.ExpectedProductionKwh)
            .Map(dest => dest.VariancePercentage, src => src.ExpectedProductionKwh > 0
                ? ((src.ActualProductionKwh - src.ExpectedProductionKwh) / src.ExpectedProductionKwh) * 100
                : 0)
            .Map(dest => dest.HealthStatus, src => src.HealthStatus.ToString())
            .Map(dest => dest.WeatherCondition, src => src.WeatherData != null ? src.WeatherData.Condition : null);

        // Entity to EnergyProductionChartResponse
        config.NewConfig<EnergyProduction, EnergyProductionChartResponse>()
            .Map(dest => dest.Date, src => src.ProductionDate)
            .Map(dest => dest.ActualKwh, src => src.ActualProductionKwh)
            .Map(dest => dest.ExpectedKwh, src => src.ExpectedProductionKwh)
            .Map(dest => dest.WeatherCondition, src => src.WeatherData != null ? src.WeatherData.Condition : null);

        // CreateEnergyProductionRequest to Entity
        config.NewConfig<CreateEnergyProductionRequest, EnergyProduction>()
            .Map(dest => dest.HealthStatus, src => Enum.Parse<SystemHealthStatus>(src.HealthStatus))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.WeatherData);

        // UpdateEnergyProductionRequest to Entity
        config.NewConfig<UpdateEnergyProductionRequest, EnergyProduction>()
            .IgnoreNullValues(true)
            .Map(dest => dest.HealthStatus, src => src.HealthStatus != null ? Enum.Parse<SystemHealthStatus>(src.HealthStatus) : SystemHealthStatus.Good)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.InstallationId)
            .Ignore(dest => dest.ProductionDate)
            .Ignore(dest => dest.WeatherDataId)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.WeatherData);
    }
}