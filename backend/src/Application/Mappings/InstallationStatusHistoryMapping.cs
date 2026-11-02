using Application.DTOs.InstallationStatusHistory;
using Domain.Entities;
using Domain.Enums;
using Mapster;

namespace Application.Mappings;

public class InstallationStatusHistoryMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity to InstallationStatusHistoryResponse
        config.NewConfig<InstallationStatusHistory, InstallationStatusHistoryResponse>()
            .Map(dest => dest.FromStatus, src => src.FromStatus.ToString())
            .Map(dest => dest.ToStatus, src => src.ToStatus.ToString())
            .Map(dest => dest.ChangedByUser, src => $"{src.ChangedBy.FirstName} {src.ChangedBy.LastName}")
            .Map(dest => dest.DaysInPreviousStatus, src => CalculateDaysInPreviousStatus(src));

        // CreateInstallationStatusHistoryRequest to Entity
        config.NewConfig<CreateInstallationStatusHistoryRequest, InstallationStatusHistory>()
            .Map(dest => dest.ToStatus, src => Enum.Parse<InstallationStatus>(src.ToStatus))
            .Map(dest => dest.ChangedAt, src => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.FromStatus)
            .Ignore(dest => dest.Installation)
            .Ignore(dest => dest.ChangedBy);
    }

    private static int CalculateDaysInPreviousStatus(InstallationStatusHistory history)
    {
        var previousHistory = history.Installation?.StatusHistory
            .Where(h => h.ChangedAt < history.ChangedAt)
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefault();

        if (previousHistory == null)
            return (history.ChangedAt - history.Installation!.StartDate).Days;

        return (history.ChangedAt - previousHistory.ChangedAt).Days;
    }
}