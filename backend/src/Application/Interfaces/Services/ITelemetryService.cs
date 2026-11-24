using Application.Common.Models;
using Application.DTOs.Telemetry;

namespace Application.Interfaces.Services;

/// <summary>A device's readings for its own installation (FR-T1, FR-T2, BR-T1).</summary>
public interface ITelemetryService
{
    /// <summary>
    /// Stores a batch for the installation its key named: how many slots were new, how many it already had, and
    /// which readings the rules refused.
    /// </summary>
    Task<Result<UploadReadingsResponse>> UploadAsync(int installationId, UploadReadingsRequest request);
}
