using Application.Common.Models;
using Domain.Enums;

namespace Application.Errors;

public static class LeadErrors
{
    public static Error NotFound(int id) =>
        new(ErrorKind.NotFound, "lead.not_found", $"Lead with ID {id} not found.");

    /// <summary>A move BR-L2 doesn't allow from the lead's status now, often because another Admin moved it first.</summary>
    public static Error TransitionNotAllowed(LeadStatus from, LeadStatus to) =>
        new(ErrorKind.Conflict, "lead.transition_not_allowed", to == LeadStatus.Converted
            ? $"This lead is {from}: only a Contacted lead can be converted."
            : $"This lead is {from}: it can't become {to}.");

    /// <summary>Only an open lead's contact details can be corrected (FR-L7).</summary>
    public static Error Closed(LeadStatus status) =>
        new(ErrorKind.Conflict, "lead.closed", $"This lead is {status}: only an open lead (New or Contacted) can be edited.");

    /// <summary>Someone saved the lead after this request read it (a conversion and a move at the same moment, BR-L3).</summary>
    public static readonly Error ConcurrentChange =
        new(ErrorKind.Conflict, "lead.concurrent_change", "This lead was changed by someone else meanwhile. Reload it to see it as it is now.");

    /// <summary>One open lead per email (BR-L1).</summary>
    public static readonly Error EmailTaken =
        new(ErrorKind.Conflict, "lead.email_taken", "Another open lead has this email.");
}
