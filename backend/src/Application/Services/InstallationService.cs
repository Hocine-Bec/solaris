using System.Diagnostics;
using Application.Common.Models;
using Application.Common.Security;
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
// BR-I4: every change names the version of the installation it was made on (the If-Match the page sent). A change made
// on an older version is refused before it is tried; one that loses a race at the same moment is refused by the save,
// which updates the installation's row only where its version is still the one read. Every change saves that row,
// even one that only touches an assignment, so its version moves with each change.
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

    public async Task<Result<InstallationResponse>> MoveAsync(int id, uint version, MoveInstallationRequest request,
        int actingUserId)
    {
        // Switching on makes its device key in the same save (BR-T2); the answer carries it, once
        NewDeviceKey? key = null;
        var result = await ChangeStatusAsync(id, version, (i, now) =>
        {
            var to = Enum.Parse<InstallationStatus>(request.Status);
            if (i.TryMoveTo(to, actingUserId, now) is { } refusal)
                return refusal;
            if (to == InstallationStatus.Active)
            {
                key = DeviceKeys.Create();
                i.SetDeviceKey(key.Prefix, key.Hash, now);
                outbox.Add(OutboxMessage.ForInstallation(i, OutboxMessageType.InstallationActivated, now));
            }
            return null;
        });
        return WithKey(result, key);
    }

    public async Task<Result<InstallationResponse>> ReplaceDeviceKeyAsync(int id, uint version)
    {
        var key = DeviceKeys.Create();
        return WithKey(await ChangeAsync(id, version, (i, now) => i.SetDeviceKey(key.Prefix, key.Hash, now)), key);
    }

    public Task<Result<InstallationResponse>> CancelAsync(int id, uint version, CancelInstallationRequest request,
        int actingUserId) =>
        ChangeStatusAsync(id, version, (i, now) => i.Cancel(request.Reason, actingUserId, now));

    public Task<Result<InstallationResponse>> DeactivateAsync(int id, uint version,
        DeactivateInstallationRequest request, int actingUserId) =>
        ChangeStatusAsync(id, version, (i, now) => i.Deactivate(request.Note, actingUserId, now));

    public Task<Result<InstallationResponse>> RecordDesignAsync(int id, uint version, InstallationDesignRequest request) =>
        ChangeAsync(id, version, (i, _) => i.RecordDesign(request.SystemSizeKw, request.PanelCount, request.InverterType));

    public Task<Result<InstallationResponse>> SetTimeZoneAsync(int id, uint version, InstallationTimeZoneRequest request) =>
        ChangeAsync(id, version, (i, _) => i.SetTimeZone(request.TimeZone));

    public async Task<Result<InstallationResponse>> AssignAsync(int id, uint version, AssignTechnicianRequest request,
        int actingUserId)
    {
        var loaded = await LoadAsync(id, version);
        if (!loaded.IsSuccess)
            return Result.Failure<InstallationResponse>(loaded.Error);
        if (await users.GetByIdAsync(request.TechnicianId) is not { } technician)
            return Result.Failure<InstallationResponse>(InstallationErrors.NotATechnician);

        var installation = loaded.Value;
        var refusal = installation.Assign(technician, Enum.Parse<AssignmentRole>(request.Role), actingUserId, Now());
        return refusal is not null
            ? Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal))
            : await SaveAsync(installation);
    }

    public async Task<Result<InstallationResponse>> UnassignAsync(int id, uint version, int assignmentId)
    {
        var loaded = await LoadAsync(id, version);
        if (!loaded.IsSuccess)
            return Result.Failure<InstallationResponse>(loaded.Error);
        var installation = loaded.Value;
        if (installation.Assignments.Find(a => a.Id == assignmentId) is not { } assignment)
            return Result.Failure<InstallationResponse>(InstallationErrors.AssignmentNotFound(assignmentId));

        return installation.Unassign(assignment) is { } refusal
            ? Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal))
            : await SaveAsync(installation);
    }

    public async Task<Result<InstallationResponse>> CompleteAsync(int id, uint version, int assignmentId, int actingUserId)
    {
        var (installation, error) = await CompleteAssignmentAsync(id, version, assignmentId, actingUserId, technicianId: null);
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

    public async Task<Result<MyWorkInstallationResponse>> CompleteMineAsync(int installationId, uint version,
        int assignmentId, int technicianId)
    {
        var (installation, error) =
            await CompleteAssignmentAsync(installationId, version, assignmentId, technicianId, technicianId);
        if (error is not null)
            return Result.Failure<MyWorkInstallationResponse>(error);
        installations.MarkChanged(installation!);
        return await unitOfWork.SaveAsync() switch
        {
            SaveOutcome.Saved => Result.Success(
                MyWorkInstallationResponse.From((await installations.GetDetailsAsync(installationId))!, technicianId)),
            SaveOutcome.Conflict => Result.Failure<MyWorkInstallationResponse>(InstallationErrors.ConcurrentChange),
            var other => throw new UnreachableException($"Completing an assignment can't end as {other}.")
        };
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
    private async Task<(Installation? Installation, Error? Error)> CompleteAssignmentAsync(int id, uint version,
        int assignmentId, int actingUserId, int? technicianId)
    {
        var installation = await installations.GetDetailsAsync(id);
        if (installation is null || (technicianId is { } mine && installation.Assignments.All(a => a.TechnicianId != mine)))
            return (null, InstallationErrors.NotFound(id));
        if (installation.Version != version)
            return (null, InstallationErrors.ConcurrentChange);
        var assignment = installation.Assignments.Find(a => a.Id == assignmentId);
        if (assignment is null || (technicianId is { } me && assignment.TechnicianId != me))
            return (null, InstallationErrors.AssignmentNotFound(assignmentId));

        return installation.Complete(assignment, actingUserId, Now()) is { } refusal
            ? (null, InstallationErrors.Refused(refusal))
            : (installation, null);
    }

    /// <summary>The installation, if it exists and is still at the version the change was made on.</summary>
    private async Task<Result<Installation>> LoadAsync(int id, uint version)
    {
        var installation = await installations.GetDetailsAsync(id);
        if (installation is null)
            return Result.Failure<Installation>(InstallationErrors.NotFound(id));
        return installation.Version == version
            ? Result.Success(installation)
            : Result.Failure<Installation>(InstallationErrors.ConcurrentChange);
    }

    private async Task<Result<InstallationResponse>> ChangeStatusAsync(int id, uint version,
        Func<Installation, DateTime, Refusal?> change)
    {
        var loaded = await LoadAsync(id, version);
        if (!loaded.IsSuccess)
            return Result.Failure<InstallationResponse>(loaded.Error);
        var installation = loaded.Value;
        if (change(installation, Now()) is { } refusal)
            return Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal));

        // BR-C1: from this installation's new status and the others' stored ones
        var others = await installations.GetStatusesOfCustomerAsync(installation.CustomerId, installation.Id);
        installation.Customer.Status = Customer.StatusFor([.. others, installation.Status]);
        return await SaveAsync(installation);
    }

    private async Task<Result<InstallationResponse>> ChangeAsync(int id, uint version,
        Func<Installation, DateTime, Refusal?> change)
    {
        var loaded = await LoadAsync(id, version);
        if (!loaded.IsSuccess)
            return Result.Failure<InstallationResponse>(loaded.Error);
        return change(loaded.Value, Now()) is { } refusal
            ? Result.Failure<InstallationResponse>(InstallationErrors.Refused(refusal))
            : await SaveAsync(loaded.Value);
    }

    private async Task<Result<InstallationResponse>> SaveAsync(Installation installation)
    {
        installations.MarkChanged(installation);
        return await unitOfWork.SaveAsync() switch
        {
            // Read again, so the people a new row names (who moved it, who assigned) are loaded with it
            SaveOutcome.Saved => Result.Success(InstallationResponse.From((await installations.GetDetailsAsync(installation.Id))!)),
            // An assignment is unique: the same technician in the same role, added twice at the same moment. (A device
            // key's prefix is unique too, but two alike out of 60 random bits is out of reach.)
            SaveOutcome.Duplicate => Result.Failure<InstallationResponse>(InstallationErrors.DuplicateAssignment),
            // Someone saved it between this change's read and its save
            SaveOutcome.Conflict => Result.Failure<InstallationResponse>(InstallationErrors.ConcurrentChange),
            var other => throw new UnreachableException($"Saving an installation can't end as {other}.")
        };
    }

    /// <summary>The answer, with the key just made in it: the only answer that ever carries it.</summary>
    private static Result<InstallationResponse> WithKey(Result<InstallationResponse> result, NewDeviceKey? key) =>
        result.IsSuccess && key is not null ? Result.Success(result.Value with { IssuedKey = key.Text }) : result;

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
