using Application.Common.Models;
using Application.DTOs.Telemetry;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

// The readings a device uploads (FR-T1, FR-T2). The rules are the Domain's (EnergyReading.WhyNotAccepted, BR-T1):
// this service asks them, then stores what is left.
//
// The whole batch goes in one statement, and the slots the installation already has are passed over by the table's
// own key rather than by a check this code makes: two devices sending one slot at the same moment can no longer
// both store it (BR-T1). The local days the batch touched are marked in the same statement, for the rollups
// (BR-P3). P4a measures the difference from the naive version, which asked and saved one reading at a time.
public class TelemetryService(
    IEnergyReadingRepo readings,
    IInstallationRepo installations,
    TimeProvider time) : ITelemetryService
{
    private const decimal WhPerKwh = 1000m;

    public async Task<Result<UploadReadingsResponse>> UploadAsync(int installationId, UploadReadingsRequest request)
    {
        // The key said which installation, and that it was Active when it was checked; it could have been deactivated
        // since, and a deactivated system's readings are no longer accepted (BR-T2).
        var installation = await installations.GetByIdAsync(installationId);
        if (installation is null || installation.Status != InstallationStatus.Active)
            return Result.Failure<UploadReadingsResponse>(TelemetryErrors.NotAccepting);

        var now = time.GetUtcNow().UtcDateTime;
        var rejected = new List<RejectedReadingResponse>();
        var wanted = new List<(DateTime SlotStart, int EnergyWh)>(request.Readings.Count);

        foreach (var reading in request.Readings)
        {
            var kwh = reading.EnergyWh / WhPerKwh;
            if (EnergyReading.WhyNotAccepted(reading.SlotStart, kwh, installation.SystemSizeKw, now) is { } refusal)
            {
                rejected.Add(new RejectedReadingResponse(reading.SlotStart, refusal.Message));
                continue;
            }
            wanted.Add((reading.SlotStart, (int)Math.Round(reading.EnergyWh)));
        }

        // A slot the installation already has, and a slot this batch repeats, are both simply not new
        var accepted = await readings.AddBatchAsync(installationId, installation.TimeZone, wanted);
        return Result.Success(new UploadReadingsResponse(accepted, wanted.Count - accepted, rejected));
    }
}
