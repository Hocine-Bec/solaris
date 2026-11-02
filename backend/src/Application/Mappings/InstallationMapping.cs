using Domain.Entities;
using Domain.Enums;
using Mapster;
using Application.DTOs.Installation;
using Application.DTOs.EnergyProduction;

namespace Application.Mappings;

public class InstallationMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to InstallationResponse
        config.NewConfig<Installation, InstallationResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CustomerName, src => $"{src.Customer.FirstName} {src.Customer.LastName}")
            .Map(dest => dest.InstallationAddress, src => src.InstallationAddress != null
                ? $"{src.InstallationAddress.Street}, {src.InstallationAddress.City}, {src.InstallationAddress.State}"
                : string.Empty)
            .Map(dest => dest.DaysInCurrentPhase, src => CalculateDaysInCurrentPhase(src));

        // Entity to InstallationListResponse
        config.NewConfig<Installation, InstallationListResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CustomerName, src => $"{src.Customer.FirstName} {src.Customer.LastName}")
            .Map(dest => dest.City, src => src.InstallationAddress != null ? src.InstallationAddress.City : string.Empty)
            .Map(dest => dest.DaysInCurrentPhase, src => CalculateDaysInCurrentPhase(src));

        // Entity to InstallationDetailResponse
        config.NewConfig<Installation, InstallationDetailResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Customer, src => src.Customer)
            .Map(dest => dest.InstallationAddress, src => src.InstallationAddress)
            .Map(dest => dest.StatusHistory, src => src.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .Map(dest => dest.AssignedTechnicians, src => src.AssignedTechnicians)
            .Map(dest => dest.Equipment, src => src.Equipment)
            .Map(dest => dest.Documents, src => src.Documents)
            .Map(dest => dest.EnergyProduction, src => CreateEnergyProductionSummary(src.EnergyProductions));

        // Entity to InstallationSummaryResponse
        config.NewConfig<Installation, InstallationSummaryResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.InstallationAddress, src => src.InstallationAddress != null
                ? $"{src.InstallationAddress.Street}, {src.InstallationAddress.City}"
                : string.Empty);

        // Entity to CustomerSummaryResponse
        config.NewConfig<Customer, CustomerSummaryResponse>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");

        // CreateInstallationRequest to Entity
        config.NewConfig<CreateInstallationRequest, Installation>()
            .Map(dest => dest.StartDate, src => DateTime.UtcNow)
            .Map(dest => dest.Status, src => InstallationStatus.Survey)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CompletionDate)
            .Ignore(dest => dest.Customer)
            .Ignore(dest => dest.InstallationAddress)
            .Ignore(dest => dest.StatusHistory)
            .Ignore(dest => dest.AssignedTechnicians)
            .Ignore(dest => dest.EnergyProductions)
            .Ignore(dest => dest.Equipment)
            .Ignore(dest => dest.Documents);

        // UpdateInstallationRequest to Entity
        config.NewConfig<UpdateInstallationRequest, Installation>()
            .IgnoreNullValues(true)
            .Map(dest => dest.Status, src => src.Status != null ? Enum.Parse<InstallationStatus>(src.Status) : InstallationStatus.Survey)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.StartDate)
            .Ignore(dest => dest.CustomerId)
            .Ignore(dest => dest.Customer)
            .Ignore(dest => dest.InstallationAddressId)
            .Ignore(dest => dest.InstallationAddress)
            .Ignore(dest => dest.StatusHistory)
            .Ignore(dest => dest.AssignedTechnicians)
            .Ignore(dest => dest.EnergyProductions)
            .Ignore(dest => dest.Equipment)
            .Ignore(dest => dest.Documents);
    }

    private static int CalculateDaysInCurrentPhase(Installation installation)
    {
        var lastStatusChange = installation.StatusHistory
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefault();

        if (lastStatusChange == null)
            return (DateTime.UtcNow - installation.StartDate).Days;

        return (DateTime.UtcNow - lastStatusChange.ChangedAt).Days;
    }

    private static EnergyProductionSummaryResponse? CreateEnergyProductionSummary(ICollection<EnergyProduction> productions)
    {
        if (!productions.Any()) return null;

        var recent = productions.OrderByDescending(p => p.ProductionDate).Take(30).ToList();
        var totalActual = recent.Sum(p => p.ActualProductionKwh);
        var totalExpected = recent.Sum(p => p.ExpectedProductionKwh);
        var best = recent.OrderByDescending(p => p.ActualProductionKwh).FirstOrDefault();

        return new EnergyProductionSummaryResponse
        {
            TotalProductionKwh = totalActual,
            AverageDailyProductionKwh = recent.Any() ? totalActual / recent.Count : 0,
            BestDayProductionKwh = best?.ActualProductionKwh ?? 0,
            BestProductionDate = best?.ProductionDate,
            TotalExpectedKwh = totalExpected,
            OverallVariancePercentage = totalExpected > 0 ? ((totalActual - totalExpected) / totalExpected) * 100 : 0,
            DaysTracked = recent.Count,
            RecentProduction = recent.Take(7).Select(p => p.Adapt<EnergyProductionResponse>()).ToList()
        };
    }
}