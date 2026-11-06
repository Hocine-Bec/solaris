using Application.Common.Models;

namespace Application.Errors;

public static class LeadErrors
{
    public static Error NotFound(int id) =>
        new(ErrorKind.NotFound, "lead.not_found", $"Lead with ID {id} not found.");

    public static Error AlreadyConverted(int id) =>
        new(ErrorKind.Conflict, "lead.already_converted", $"Lead with ID {id} has already been converted.");

    public static readonly Error ConcurrentChange =
        new(ErrorKind.Conflict, "lead.concurrent_change", "The lead was changed by another user.");
}
