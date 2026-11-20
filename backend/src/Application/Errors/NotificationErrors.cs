using Application.Common.Models;

namespace Application.Errors;

public static class NotificationErrors
{
    public static Error NotFound(Guid id) =>
        new(ErrorKind.NotFound, "notification.not_found", $"Notification {id} not found.");

    /// <summary>Only a Failed email can be retried: another Admin, or a second tab, retried it first, or it was sent.</summary>
    public static readonly Error NotFailed =
        new(ErrorKind.Conflict, "notification.not_failed",
            "This email isn't waiting for a retry: it was already retried, or sent.");
}
