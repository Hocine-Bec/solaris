using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One solar project for one customer at one address, from the site visit to a working system. A conversion creates
/// it at Survey, at the lead's property (BR-L3). Its rules live here: the stages and their conditions (BR-I1),
/// cancelling and deactivating (BR-I2), a history row for every status change (BR-I3), its assignments (FR-I5, FR-I6),
/// and its design and time zone, fixed once it is Active (BR-P4). Each change returns a <see cref="Refusal"/>, or null
/// when it was made.
/// </summary>
public class Installation
{
    public const decimal MinSystemSizeKw = 0.5m, MaxSystemSizeKw = 100m;
    public const int MinPanelCount = 1, MaxPanelCount = 400, MaxInverterTypeLength = 100, MaxNoteLength = 500;

    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int AddressId { get; set; }
    public Address Address { get; set; } = null!;

    /// <summary>The lead whose conversion created it.</summary>
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public InstallationStatus Status { get; set; }

    /// <summary>When its status last changed: the list's order.</summary>
    public DateTime StatusChangedAt { get; set; }

    /// <summary>
    /// Its IANA time zone, in which its production is counted per local day: proposed from its state at conversion
    /// (<see cref="UsTimeZones"/>), then confirmed by the Admin when switching it on (BR-P4).
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    // The design (FR-I4): needed to leave Design, fixed once Active
    public decimal? SystemSizeKw { get; set; }
    public int? PanelCount { get; set; }
    public string? InverterType { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>When it was switched on.</summary>
    public DateTime? ActivatedAt { get; set; }

    public List<InstallationStatusChange> History { get; set; } = [];
    public List<InstallationAssignment> Assignments { get; set; } = [];

    public bool IsClosed => Status is InstallationStatus.Cancelled or InstallationStatus.Deactivated;

    /// <summary>Active or closed: its design, its time zone and its assignments no longer change.</summary>
    public bool IsFixed => Status == InstallationStatus.Active || IsClosed;

    /// <summary>The stage after this one, while it is in progress; null once it is Active or closed.</summary>
    public InstallationStatus? NextStage => Status switch
    {
        InstallationStatus.Survey => InstallationStatus.Design,
        InstallationStatus.Design => InstallationStatus.Permits,
        InstallationStatus.Permits => InstallationStatus.Installation,
        InstallationStatus.Installation => InstallationStatus.Inspection,
        InstallationStatus.Inspection => InstallationStatus.Active,
        _ => null
    };

    public bool CanCancel => !IsFixed;
    public bool CanDeactivate => Status == InstallationStatus.Active;

    /// <summary>
    /// A new installation for a converted lead: at Survey, at the lead's property, in its state's time zone, with the
    /// first history row, its creation.
    /// </summary>
    public static Installation ForConvertedLead(Lead lead, int customerId, string timeZone, int actingUserId,
        DateTime now) => new()
    {
        CustomerId = customerId,
        AddressId = lead.AddressId,
        LeadId = lead.Id,
        Status = InstallationStatus.Survey,
        StatusChangedAt = now,
        TimeZone = timeZone,
        CreatedAt = now,
        History = [new InstallationStatusChange { ToStatus = InstallationStatus.Survey, ChangedAt = now, ChangedByUserId = actingUserId }]
    };

    // ---- The stages (BR-I1) ----

    /// <summary>What the next stage waits for, in the office's words; null when its condition is met (BR-I1).</summary>
    public string? WaitingFor(InstallationStatus next) => next switch
    {
        InstallationStatus.Design => WaitingForPart(AssignmentRole.Surveyor, "Assign a surveyor, and have them complete the survey.", "the survey"),
        InstallationStatus.Permits => SystemSizeKw > 0 && PanelCount > 0 ? null : "Record the system size and the panel count first.",
        InstallationStatus.Installation => Of(AssignmentRole.Installer).Any() ? null : "Assign at least one installer first.",
        InstallationStatus.Inspection => !Of(AssignmentRole.Installer).Any()
            ? "Assign at least one installer first."
            : Of(AssignmentRole.Installer).FirstOrDefault(a => !a.IsCompleted) is { } waiting
                ? $"Every installer must complete their part: {waiting.Technician.Name} hasn't yet."
                : null,
        InstallationStatus.Active => WaitingForPart(AssignmentRole.Inspector, "Assign an inspector, and have them complete the inspection.", "the inspection"),
        _ => null
    };

    /// <summary>Why this installation can't move to <paramref name="to"/> now; null if it can.</summary>
    public Refusal? WhyNotMoveTo(InstallationStatus to)
    {
        if (NextStage is not { } next)
            return NotAllowed($"This installation is {Status}: it can't move to {to}.");
        if (to != next)
            return NotAllowed($"This installation is at {Status}: it can only move to {next}.");
        return WaitingFor(to) is { } reason ? NotAllowed($"Can't move to {to} yet. {reason}") : null;
    }

    /// <summary>Moves it one stage on, if its condition is met; switching on records when (BR-I1, BR-I3).</summary>
    public Refusal? TryMoveTo(InstallationStatus to, int actingUserId, DateTime now)
    {
        if (WhyNotMoveTo(to) is { } refusal)
            return refusal;
        Record(to, actingUserId, now, null);
        if (to == InstallationStatus.Active)
            ActivatedAt = now;
        return null;
    }

    /// <summary>Cancelled, from any stage before Active, with a reason: the note of its history row (BR-I2).</summary>
    public Refusal? Cancel(string reason, int actingUserId, DateTime now)
    {
        if (!CanCancel)
            return NotAllowed($"This installation is {Status}: only one before Active can be cancelled.");
        Record(InstallationStatus.Cancelled, actingUserId, now, reason.Trim());
        return null;
    }

    /// <summary>Deactivated, only from Active (BR-I2). The device key is revoked here once keys exist.</summary>
    public Refusal? Deactivate(string? note, int actingUserId, DateTime now)
    {
        if (!CanDeactivate)
            return NotAllowed($"This installation is {Status}: only an Active one can be deactivated.");
        Record(InstallationStatus.Deactivated, actingUserId, now, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
        return null;
    }

    // ---- The design and the time zone (FR-I4, BR-P4) ----

    public Refusal? RecordDesign(decimal systemSizeKw, int panelCount, string? inverterType)
    {
        if (IsFixed)
            return Fixed();
        SystemSizeKw = systemSizeKw;
        PanelCount = panelCount;
        InverterType = string.IsNullOrWhiteSpace(inverterType) ? null : inverterType.Trim();
        return null;
    }

    public Refusal? SetTimeZone(string timeZone)
    {
        if (UsTimeZones.All.All(z => z.Id != timeZone))
            throw new ArgumentException($"'{timeZone}' isn't one of the US time zones.", nameof(timeZone));
        if (IsFixed)
            return Fixed();
        TimeZone = timeZone;
        return null;
    }

    // ---- The assignments (FR-I5, FR-I6) ----

    /// <summary>An active technician, in a role they don't already have here.</summary>
    public Refusal? Assign(User technician, AssignmentRole role, int actingUserId, DateTime now)
    {
        if (IsFixed)
            return new Refusal("installation.closed", $"This installation is {Status}: nobody can be assigned to it any more.");
        if (technician.Role != UserRole.Technician || !technician.IsActive)
            return new Refusal("assignment.not_a_technician", "Only an active technician can be assigned.");
        if (Assignments.Any(a => a.TechnicianId == technician.Id && a.Role == role))
            return new Refusal("assignment.duplicate", $"{technician.Name} is already assigned as {role}.");

        Assignments.Add(new InstallationAssignment
        {
            TechnicianId = technician.Id, Technician = technician, Role = role, AssignedAt = now, AssignedByUserId = actingUserId
        });
        return null;
    }

    /// <summary>A mistaken assignment goes; a completed one stays, as a move may have relied on it.</summary>
    public Refusal? Unassign(InstallationAssignment assignment)
    {
        if (IsFixed)
            return new Refusal("installation.closed", $"This installation is {Status}: its assignments no longer change.");
        if (assignment.IsCompleted)
            return new Refusal("assignment.completed", $"{assignment.Technician.Name}'s part is completed: it stays on record.");
        Assignments.Remove(assignment);
        return null;
    }

    /// <summary>Why this part can't be marked complete now; null if it can (only at its stage, point 04).</summary>
    public Refusal? WhyNotComplete(InstallationAssignment assignment)
    {
        if (assignment.IsCompleted)
            return new Refusal("assignment.completed", $"{assignment.Technician.Name}'s part is already completed.");
        if (Status != assignment.Stage)
            return new Refusal("assignment.not_at_its_stage",
                $"The {assignment.Role.ToString().ToLowerInvariant()}'s part is completed at {assignment.Stage}; this installation is {(IsFixed ? "" : "at ")}{Status}.");
        return null;
    }

    /// <summary>Marked complete by its technician, or by an Admin for them; the assignment keeps who and when.</summary>
    public Refusal? Complete(InstallationAssignment assignment, int actingUserId, DateTime now)
    {
        if (WhyNotComplete(assignment) is { } refusal)
            return refusal;
        assignment.CompletedAt = now;
        assignment.CompletedByUserId = actingUserId;
        return null;
    }

    // ----

    private IEnumerable<InstallationAssignment> Of(AssignmentRole role) => Assignments.Where(a => a.Role == role);

    private string? WaitingForPart(AssignmentRole role, string noneYet, string part)
    {
        var assigned = Of(role).ToList();
        if (assigned.Count == 0)
            return noneYet;
        return assigned.Any(a => a.IsCompleted) ? null : $"{assigned[0].Technician.Name} hasn't completed {part} yet.";
    }

    private void Record(InstallationStatus to, int actingUserId, DateTime now, string? note)
    {
        History.Add(new InstallationStatusChange
        {
            FromStatus = Status, ToStatus = to, ChangedAt = now, ChangedByUserId = actingUserId, Note = note
        });
        Status = to;
        StatusChangedAt = now;
    }

    private static Refusal NotAllowed(string message) => new("installation.transition_not_allowed", message);

    private Refusal Fixed() =>
        new("installation.fixed", $"This installation is {Status}: its design and time zone no longer change.");
}
