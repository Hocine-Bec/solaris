using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Installations;

namespace Application.Interfaces.Services;

/// <summary>
/// Installations (FR-I1–I7): the office's list and pages, their moves, design, time zone and assignments; a
/// technician's own work (FR-I6); and a technician's assignments under Users (FR-U4). Each change answers the page.
/// </summary>
public interface IInstallationService
{
    Task<PagedResponse<InstallationSummaryResponse>> GetPageAsync(InstallationPageRequest request);
    Task<Result<InstallationResponse>> GetByIdAsync(int id);
    Task<Result<InstallationResponse>> MoveAsync(int id, MoveInstallationRequest request, int actingUserId);
    Task<Result<InstallationResponse>> CancelAsync(int id, CancelInstallationRequest request, int actingUserId);
    Task<Result<InstallationResponse>> DeactivateAsync(int id, DeactivateInstallationRequest request, int actingUserId);
    Task<Result<InstallationResponse>> RecordDesignAsync(int id, InstallationDesignRequest request);
    Task<Result<InstallationResponse>> SetTimeZoneAsync(int id, InstallationTimeZoneRequest request);
    Task<Result<InstallationResponse>> AssignAsync(int id, AssignTechnicianRequest request, int actingUserId);
    Task<Result<InstallationResponse>> UnassignAsync(int id, int assignmentId);

    /// <summary>An Admin marks a technician's part complete for them (FR-I6).</summary>
    Task<Result<InstallationResponse>> CompleteAsync(int id, int assignmentId, int actingUserId);

    Task<IReadOnlyList<MyWorkItemResponse>> GetMyWorkAsync(int technicianId);
    Task<Result<MyWorkInstallationResponse>> GetMyInstallationAsync(int installationId, int technicianId);

    /// <summary>A technician marks their own part complete (FR-I6).</summary>
    Task<Result<MyWorkInstallationResponse>> CompleteMineAsync(int installationId, int assignmentId, int technicianId);

    Task<IReadOnlyList<UserAssignmentResponse>> GetAssignmentsOfAsync(int technicianId);
}
