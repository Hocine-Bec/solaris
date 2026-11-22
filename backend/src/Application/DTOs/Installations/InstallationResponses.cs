using System.Text.Json.Serialization;
using Application.DTOs.Common;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Installations;

/// <summary>Someone named on a page: an id and a name.</summary>
public sealed record PersonResponse(int Id, string Name)
{
    public static PersonResponse? From(User? user) => user is null ? null : new(user.Id, user.Name);
}

/// <summary>An installation in the list (FR-I1): where, whose, its stage, and when it last changed.</summary>
public sealed record InstallationSummaryResponse(
    int Id,
    AddressResponse Address,
    PersonResponse Customer,
    string Status,
    DateTime CreatedAt,
    DateTime StatusChangedAt)
{
    public static InstallationSummaryResponse From(Installation i) =>
        new(i.Id, AddressResponse.From(i.Address), new PersonResponse(i.Customer.Id, i.Customer.FullName),
            i.Status.ToString(), i.CreatedAt, i.StatusChangedAt);
}

/// <summary>
/// One installation, as its page shows it (FR-I1–I6): its customer and their status, its stage and the next move with
/// the API's reason while it waits (BR-I1), what can be done now, its design and time zone (and whether they are fixed),
/// its assignments, and its history, newest first: the status changes, and the assignments made and completed. Its
/// <see cref="Version"/> goes in the <c>ETag</c> header, for the next change to send back (BR-I4).
/// </summary>
public sealed record InstallationResponse(
    int Id,
    AddressResponse Address,
    InstallationCustomerResponse Customer,
    int LeadId,
    string Status,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    InstallationTimeZoneResponse TimeZone,
    IReadOnlyList<TimeZoneOption> TimeZones,
    InstallationDesignResponse Design,
    NextMoveResponse? Next,
    bool CanCancel,
    bool CanDeactivate,
    IReadOnlyList<AssignmentResponse> Assignments,
    IReadOnlyList<HistoryEntryResponse> History,
    [property: JsonIgnore] uint Version)
{
    public static InstallationResponse From(Installation i)
    {
        var zone = UsTimeZones.All.FirstOrDefault(z => z.Id == i.TimeZone);
        var next = i.NextStage is { } stage
            ? new NextMoveResponse(stage.ToString(), i.WaitingFor(stage) is null, i.WaitingFor(stage))
            : null;
        return new InstallationResponse(
            i.Id, AddressResponse.From(i.Address),
            new InstallationCustomerResponse(i.Customer.Id, i.Customer.FullName, i.Customer.Status.ToString()),
            i.LeadId, i.Status.ToString(), i.CreatedAt, i.ActivatedAt,
            new InstallationTimeZoneResponse(i.TimeZone, zone.Name ?? i.TimeZone, i.IsFixed),
            i.IsFixed ? [] : UsTimeZones.All.Select(z => new TimeZoneOption(z.Id, z.Name)).ToList(),
            new InstallationDesignResponse(i.SystemSizeKw, i.PanelCount, i.InverterType, i.IsFixed),
            next, i.CanCancel, i.CanDeactivate,
            i.Assignments.OrderBy(a => a.AssignedAt).ThenBy(a => a.Id).Select(a => AssignmentResponse.From(i, a)).ToList(),
            HistoryOf(i), i.Version);
    }

    // The status changes and the assignments' events, newest first; at one moment, the status change first
    private static List<HistoryEntryResponse> HistoryOf(Installation i) =>
        i.History.Select(h => new HistoryEntryResponse(h.ChangedAt, "Status", h.FromStatus?.ToString(), h.ToStatus.ToString(),
                PersonResponse.From(h.ChangedBy), h.Note, null, null))
            .Concat(i.Assignments.Select(a => new HistoryEntryResponse(a.AssignedAt, "Assigned", null, null,
                PersonResponse.From(a.AssignedBy), null, PersonResponse.From(a.Technician), a.Role.ToString())))
            .Concat(i.Assignments.Where(a => a.IsCompleted).Select(a => new HistoryEntryResponse(a.CompletedAt!.Value,
                "Completed", null, null, PersonResponse.From(a.CompletedBy), null, PersonResponse.From(a.Technician), a.Role.ToString())))
            .OrderByDescending(e => e.At).ThenBy(e => e.Kind == "Status" ? 0 : e.Kind == "Completed" ? 1 : 2)
            .ToList();
}

public sealed record InstallationCustomerResponse(int Id, string Name, string Status);

/// <summary>Its time zone; <c>Fixed</c> once it is Active (BR-P4).</summary>
public sealed record InstallationTimeZoneResponse(string Id, string Name, bool Fixed);

public sealed record TimeZoneOption(string Id, string Name);

public sealed record InstallationDesignResponse(decimal? SystemSizeKw, int? PanelCount, string? InverterType, bool Fixed);

/// <summary>The next stage; while it isn't allowed, why, in the office's words (BR-I1).</summary>
public sealed record NextMoveResponse(string Status, bool Allowed, string? Reason);

public sealed record AssignmentResponse(
    int Id,
    PersonResponse Technician,
    string Role,
    DateTime AssignedAt,
    PersonResponse? AssignedBy,
    DateTime? CompletedAt,
    PersonResponse? CompletedBy,
    bool CanComplete,
    bool CanRemove)
{
    public static AssignmentResponse From(Installation i, InstallationAssignment a) =>
        new(a.Id, PersonResponse.From(a.Technician)!, a.Role.ToString(), a.AssignedAt, PersonResponse.From(a.AssignedBy),
            a.CompletedAt, PersonResponse.From(a.CompletedBy), i.WhyNotComplete(a) is null, !i.IsFixed && !a.IsCompleted);
}

/// <summary>
/// One entry of the history: a status change (<c>Status</c>: from, to, by, note) or an assignment's event
/// (<c>Assigned</c> or <c>Completed</c>: the technician, the role, and who did it).
/// </summary>
public sealed record HistoryEntryResponse(
    DateTime At,
    string Kind,
    string? From,
    string? To,
    PersonResponse? By,
    string? Note,
    PersonResponse? Technician,
    string? Role);

/// <summary>One installation a technician is assigned to, in My work (FR-I6).</summary>
public sealed record MyWorkItemResponse(
    int InstallationId,
    AddressResponse Address,
    string CustomerName,
    string Status,
    string Role,
    int AssignmentId,
    DateTime? CompletedAt);

/// <summary>
/// One installation as its technician sees it: the property, the customer's name and phone (point 08), its stage, their
/// own assignments (and whether each can be marked complete now, or why not), and who else works on it.
/// </summary>
public sealed record MyWorkInstallationResponse(
    int InstallationId,
    AddressResponse Address,
    MyWorkCustomerResponse Customer,
    string Status,
    IReadOnlyList<MyAssignmentResponse> MyAssignments,
    IReadOnlyList<TeamMemberResponse> Team,
    [property: JsonIgnore] uint Version)
{
    public static MyWorkInstallationResponse From(Installation i, int technicianId) =>
        new(i.Id, AddressResponse.From(i.Address), new MyWorkCustomerResponse(i.Customer.FullName, i.Customer.PhoneNumber),
            i.Status.ToString(),
            i.Assignments.Where(a => a.TechnicianId == technicianId).OrderBy(a => a.Id)
                .Select(a => new MyAssignmentResponse(a.Id, a.Role.ToString(), a.CompletedAt, i.WhyNotComplete(a) is null,
                    a.IsCompleted ? null : i.WhyNotComplete(a)?.Message))
                .ToList(),
            i.Assignments.Where(a => a.TechnicianId != technicianId).OrderBy(a => a.Id)
                .Select(a => new TeamMemberResponse(a.Technician.Name, a.Role.ToString(), a.CompletedAt)).ToList(),
            i.Version);
}

public sealed record MyWorkCustomerResponse(string Name, string Phone);

public sealed record MyAssignmentResponse(int Id, string Role, DateTime? CompletedAt, bool CanComplete, string? Why);

public sealed record TeamMemberResponse(string Name, string Role, DateTime? CompletedAt);

/// <summary>A technician's assignment, on their page under Users (FR-U4).</summary>
public sealed record UserAssignmentResponse(
    int InstallationId,
    AddressResponse Address,
    string CustomerName,
    string Status,
    string Role,
    DateTime? CompletedAt);
