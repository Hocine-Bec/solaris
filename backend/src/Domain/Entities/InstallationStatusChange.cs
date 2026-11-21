using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One status change of an installation (BR-I3): from, to, who, when, and a note (a cancellation's reason). Rows are
/// only appended, never changed. The first row of an installation has no <see cref="FromStatus"/>: its creation.
/// </summary>
public class InstallationStatusChange
{
    public int Id { get; set; }
    public int InstallationId { get; set; }
    public InstallationStatus? FromStatus { get; set; }
    public InstallationStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }

    /// <summary>Who made the change; null only for rows written for installations that existed before the history did.</summary>
    public int? ChangedByUserId { get; set; }
    public User? ChangedBy { get; set; }

    public string? Note { get; set; }
}
