using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Installations;

namespace Application.Interfaces.Services;

/// <summary>
/// Installations (FR-I1–I7): the office's list and pages, their moves, design, time zone and assignments; a
/// technician's own work (FR-I6); and a technician's assignments under Users (FR-U4). Each change answers the page.
/// Each change takes the version of the installation it was made on, and is refused as a concurrent change when the
/// installation has moved on since (BR-I4).
/// </summary>
public interface IInstallationService
{
    Task<PagedResponse<InstallationSummaryResponse>> GetPageAsync(InstallationPageRequest request);
    Task<Result<InstallationResponse>> GetByIdAsync(int id);
    Task<Result<InstallationResponse>> MoveAsync(int id, uint version, MoveInstallationRequest request, int actingUserId);
    Task<Result<InstallationResponse>> CancelAsync(int id, uint version, CancelInstallationRequest request, int actingUserId);
    Task<Result<InstallationResponse>> DeactivateAsync(int id, uint version, DeactivateInstallationRequest request, int actingUserId);
    Task<Result<InstallationResponse>> RecordDesignAsync(int id, uint version, InstallationDesignRequest request);
    Task<Result<InstallationResponse>> SetTimeZoneAsync(int id, uint version, InstallationTimeZoneRequest request);
    Task<Result<InstallationResponse>> AssignAsync(int id, uint version, AssignTechnicianRequest request, int actingUserId);
    Task<Result<InstallationResponse>> UnassignAsync(int id, uint version, int assignmentId);

    /// <summary>An Admin marks a technician's part complete for them (FR-I6).</summary>
    Task<Result<InstallationResponse>> CompleteAsync(int id, uint version, int assignmentId, int actingUserId);

    Task<IReadOnlyList<MyWorkItemResponse>> GetMyWorkAsync(int technicianId);
    Task<Result<MyWorkInstallationResponse>> GetMyInstallationAsync(int installationId, int technicianId);

    /// <summary>A technician marks their own part complete (FR-I6).</summary>
    Task<Result<MyWorkInstallationResponse>> CompleteMineAsync(int installationId, uint version, int assignmentId, int technicianId);

    Task<IReadOnlyList<UserAssignmentResponse>> GetAssignmentsOfAsync(int technicianId);
}
