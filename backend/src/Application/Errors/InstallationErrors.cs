using Application.Common.Models;
using Domain.Common;

namespace Application.Errors;

public static class InstallationErrors
{
    public static Error NotFound(int id) =>
        new(ErrorKind.NotFound, "installation.not_found", $"Installation with ID {id} not found.");

    /// <summary>Someone saved the installation after the version this change was made on (BR-I4).</summary>
    public static readonly Error ConcurrentChange =
        new(ErrorKind.Conflict, "installation.concurrent_change", "This installation was changed by someone else. Reload and try again.");

    /// <summary>A change that didn't send the version it was made on (If-Match).</summary>
    public static readonly Error VersionRequired =
        new(ErrorKind.PreconditionRequired, "installation.version_required",
            "Send the version of the installation this change was made on, in If-Match: read the installation first.");

    public static Error AssignmentNotFound(int id) =>
        new(ErrorKind.NotFound, "assignment.not_found", $"Assignment with ID {id} not found.");

    /// <summary>The Domain's refusal (a move its condition doesn't allow yet, a fixed design, a duplicate assignment): a 409.</summary>
    public static Error Refused(Refusal refusal) => new(ErrorKind.Conflict, refusal.Code, refusal.Message);

    public static readonly Error NotATechnician =
        new(ErrorKind.Conflict, "assignment.not_a_technician", "Only an active technician can be assigned.");

    /// <summary>The unique index on (installation, technician, role) refused a second assignment made at the same moment.</summary>
    public static readonly Error DuplicateAssignment =
        new(ErrorKind.Conflict, "assignment.duplicate", "This technician is already assigned in this role.");
}
