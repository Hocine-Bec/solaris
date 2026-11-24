using System.Diagnostics;
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
// Built naive on purpose: each reading is asked for and saved on its own, so two batches arriving at the same moment
// can both pass the check and store the same slot twice, and 500 readings cost 500 round trips. P4a measures both;
// the fixes branch gives the table the key that makes a duplicate impossible and writes a batch in one statement.
public class TelemetryService(
    IEnergyReadingRepo readings,
    IInstallationRepo installations,
    IUnitOfWork unitOfWork,
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
        var accepted = 0;
        var duplicates = 0;

        foreach (var reading in request.Readings)
        {
            var kwh = reading.EnergyWh / WhPerKwh;
            if (EnergyReading.WhyNotAccepted(reading.SlotStart, kwh, installation.SystemSizeKw, now) is { } refusal)
            {
                rejected.Add(new RejectedReadingResponse(reading.SlotStart, refusal.Message));
                continue;
            }

            if (await readings.ExistsAsync(installationId, reading.SlotStart))
            {
                duplicates++;
                continue;
            }

            readings.Add(new EnergyReading
            {
                InstallationId = installationId,
                RecordedAt = reading.SlotStart,
                EnergyKwh = kwh
            });
            var outcome = await unitOfWork.SaveAsync();
            if (outcome != SaveOutcome.Saved)
                throw new UnreachableException($"Storing a reading can't end as {outcome}: nothing about it is unique yet.");
            accepted++;
        }

        return Result.Success(new UploadReadingsResponse(accepted, duplicates, rejected));
    }
}
