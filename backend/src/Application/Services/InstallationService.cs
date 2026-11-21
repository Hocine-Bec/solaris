using System.Diagnostics;
using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Installations;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

// The rules are the Domain's (Installation): this service loads the installation with what they need, asks it, and saves
// once. Every status change recalculates the customer's status in the same save (BR-C1); switching on saves the
// customer's email with it (BR-N1, FR-I7).
//
// Built naive on purpose: no concurrency token yet, so two changes made at the same moment can both be saved. P3b
// measures that; the fixes branch adds the token and If-Match (BR-I4).
public class InstallationService(
    IInstallationRepo installations,
    IUserRepo users,
    IOutboxRepo outbox,
    IUnitOfWork unitOfWork,
    TimeProvider time) : IInstallationService
{
    public async Task<PagedResponse<InstallationSummaryResponse>> GetPageAsync(InstallationPageRequest request)
    {
        var statuses = request.Status.Select(Enum.Parse<InstallationStatus>).ToHashSet();
        var (items, totalCount) = await installations.GetPageAsync(statuses, request.Offset(), request.PageSize);
        return new PagedResponse<InstallationSummaryResponse>(items.Select(InstallationSummaryResponse.From).ToList(),
            request.Page, request.PageSize, totalCount);
    }

    public async Task<Result<InstallationResponse>> GetByIdAsync(int id)
    {
        var installation = await installations.GetDetailsAsync(id);
        return installation is null
            ? Result.Failure<InstallationResponse>(InstallationErrors.NotFound(id))
            : Result.Success(InstallationResponse.From(installation));
    }

    public Task<Result<InstallationResponse>> MoveAsync(int id, MoveInstallationRequest request, int actingUserId) =>
        ChangeStatusAsync(id, (i, now) =>
        {
            var to = Enum.Parse<InstallationStatus>(request.Status);
            if (i.TryMoveTo(to, actingUserId, now) is { } refusal)
                return refusal;
            if (to == InstallationStatus.Active)
                outbox.Add(OutboxMessage.ForInstallation(i, OutboxMessageType.InstallationActivated, now));
            return null;
        });

    public Task<Result<InstallationResponse>> CancelAsync(int id, CancelInstallationRequest request, int actingUserId) =>
        ChangeStatusAsync(id, (i, now) => i.Cancel(request.Reason, actingUserId, now));

    public Task<Result<InstallationResponse>> DeactivateAsync(int id, DeactivateInstallationRequest request,
        int actingUserId) =>
        ChangeStatusAsync(id, (i, now) => i.Deactivate(request.Note, actingUserId, now));

    public Task<Result<InstallationResponse>> RecordDesignAsync(int id, InstallationDesignRequest request) =>
        ChangeAsync(id, (i, _) => i.RecordDesign(request.SystemSizeKw, request.PanelCount, request.InverterType));

    public Task<Result<InstallationResponse>> SetTimeZoneAsync(int id, InstallationTimeZoneRequest request) =>
        ChangeAsync(id, (i, _) => i.SetTimeZone(request.TimeZone));

    public async Task<Result<InstallationResponse>> AssignAsync(int id, AssignTechnicianRequest request, int actingUserId)
    {
        var installation = await installations.GetDetailsAsync(id);
        if (installation is null)
            return Result.Failure<InstallationResponse>(InstallationErrors.NotFound(id));
        if (await users.GetByIdAsync(request.TechnicianId) is not { } technician)
            return Result.Failure<InstallationResponse>(InstallationErrors.NotATechnician);

        var refusal = installation.Assign(technician, Enum.Parse<AssignmentRole>(request.Role), actingUserId, Now());
        return refusal is not null
            ? Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal))
            : await SaveAsync(installation);
    }

    public async Task<Result<InstallationResponse>> UnassignAsync(int id, int assignmentId)
    {
        var installation = await installations.GetDetailsAsync(id);
        if (installation is null)
            return Result.Failure<InstallationResponse>(InstallationErrors.NotFound(id));
        if (installation.Assignments.Find(a => a.Id == assignmentId) is not { } assignment)
            return Result.Failure<InstallationResponse>(InstallationErrors.AssignmentNotFound(assignmentId));

        return installation.Unassign(assignment) is { } refusal
            ? Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal))
            : await SaveAsync(installation);
    }

    public async Task<Result<InstallationResponse>> CompleteAsync(int id, int assignmentId, int actingUserId)
    {
        var (installation, error) = await CompleteAssignmentAsync(id, assignmentId, actingUserId, technicianId: null);
        return error is not null ? Result.Failure<InstallationResponse>(error) : await SaveAsync(installation!);
    }

    public async Task<IReadOnlyList<MyWorkItemResponse>> GetMyWorkAsync(int technicianId)
    {
        var assigned = await installations.GetForTechnicianAsync(technicianId);
        return assigned
            .SelectMany(i => i.Assignments.Where(a => a.TechnicianId == technicianId).Select(a => (i, a)))
            .OrderBy(x => x.a.IsCompleted).ThenByDescending(x => x.a.CompletedAt ?? x.i.StatusChangedAt).ThenByDescending(x => x.a.Id)
            .Select(x => new MyWorkItemResponse(x.i.Id, AddressResponse.From(x.i.Address), x.i.Customer.FullName,
                x.i.Status.ToString(), x.a.Role.ToString(), x.a.Id, x.a.CompletedAt))
            .ToList();
    }

    public async Task<Result<MyWorkInstallationResponse>> GetMyInstallationAsync(int installationId, int technicianId)
    {
        var installation = await installations.GetDetailsAsync(installationId);
        // Another installation is not theirs to see: it doesn't exist for them
        return installation is null || installation.Assignments.All(a => a.TechnicianId != technicianId)
            ? Result.Failure<MyWorkInstallationResponse>(InstallationErrors.NotFound(installationId))
            : Result.Success(MyWorkInstallationResponse.From(installation, technicianId));
    }

    public async Task<Result<MyWorkInstallationResponse>> CompleteMineAsync(int installationId, int assignmentId,
        int technicianId)
    {
        var (installation, error) = await CompleteAssignmentAsync(installationId, assignmentId, technicianId, technicianId);
        if (error is not null)
            return Result.Failure<MyWorkInstallationResponse>(error);
        return await unitOfWork.SaveAsync() == SaveOutcome.Saved
            ? Result.Success(MyWorkInstallationResponse.From((await installations.GetDetailsAsync(installationId))!, technicianId))
            : Result.Failure<MyWorkInstallationResponse>(InstallationErrors.NotFound(installationId));
    }

    public async Task<IReadOnlyList<UserAssignmentResponse>> GetAssignmentsOfAsync(int technicianId)
    {
        var assigned = await installations.GetForTechnicianAsync(technicianId);
        return assigned
            .SelectMany(i => i.Assignments.Where(a => a.TechnicianId == technicianId).Select(a => (i, a)))
            .OrderBy(x => x.a.IsCompleted).ThenByDescending(x => x.a.AssignedAt).ThenByDescending(x => x.a.Id)
            .Select(x => new UserAssignmentResponse(x.i.Id, AddressResponse.From(x.i.Address), x.i.Customer.FullName,
                x.i.Status.ToString(), x.a.Role.ToString(), x.a.CompletedAt))
            .ToList();
    }

    // ----

    /// <summary>
    /// Marks an assignment complete, by an Admin (<paramref name="technicianId"/> null) or by its own technician: for a
    /// technician, an installation or an assignment that isn't theirs is not found.
    /// </summary>
    private async Task<(Installation? Installation, Error? Error)> CompleteAssignmentAsync(int id, int assignmentId,
        int actingUserId, int? technicianId)
    {
        var installation = await installations.GetDetailsAsync(id);
        if (installation is null || (technicianId is { } mine && installation.Assignments.All(a => a.TechnicianId != mine)))
            return (null, InstallationErrors.NotFound(id));
        var assignment = installation.Assignments.Find(a => a.Id == assignmentId);
        if (assignment is null || (technicianId is { } me && assignment.TechnicianId != me))
            return (null, InstallationErrors.AssignmentNotFound(assignmentId));

        return installation.Complete(assignment, actingUserId, Now()) is { } refusal
            ? (null, InstallationErrors.Refused(refusal))
            : (installation, null);
    }

    private async Task<Result<InstallationResponse>> ChangeStatusAsync(int id, Func<Installation, DateTime, Refusal?> change)
    {
        var installation = await installations.GetDetailsAsync(id);
        if (installation is null)
            return Result.Failure<InstallationResponse>(InstallationErrors.NotFound(id));
        if (change(installation, Now()) is { } refusal)
            return Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal));

        // BR-C1: from this installation's new status and the others' stored ones
        var others = await installations.GetStatusesOfCustomerAsync(installation.CustomerId, installation.Id);
        installation.Customer.Status = Customer.StatusFor([.. others, installation.Status]);
        return await SaveAsync(installation);
    }

    private async Task<Result<InstallationResponse>> ChangeAsync(int id, Func<Installation, DateTime, Refusal?> change)
    {
        var installation = await installations.GetDetailsAsync(id);
        if (installation is null)
            return Result.Failure<InstallationResponse>(InstallationErrors.NotFound(id));
        return change(installation, Now()) is { } refusal
            ? Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal))
            : await SaveAsync(installation);
    }

    private async Task<Result<InstallationResponse>> SaveAsync(Installation installation) =>
        await unitOfWork.SaveAsync() switch
        {
            // Read again, so the people a new row names (who moved it, who assigned) are loaded with it
            SaveOutcome.Saved => Result.Success(InstallationResponse.From((await installations.GetDetailsAsync(installation.Id))!)),
            // Only an assignment is unique: the same technician in the same role, added twice at the same moment
            SaveOutcome.Duplicate => Result.Failure<InstallationResponse>(InstallationErrors.DuplicateAssignment),
            // Installations are never deleted, and nothing here has a concurrency token yet: the row is gone
            SaveOutcome.Conflict => Result.Failure<InstallationResponse>(InstallationErrors.NotFound(installation.Id)),
            var other => throw new UnreachableException($"Saving an installation can't end as {other}.")
        };

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
