using Application.Common.Models;

namespace Application.Errors;

public static class UserErrors
{
    public static Error NotFound(int id) =>
        new(ErrorKind.NotFound, "user.not_found", $"User with ID {id} not found.");

    /// <summary>One account per email, whatever its case or spaces; a deactivated account keeps its email.</summary>
    public static readonly Error EmailTaken =
        new(ErrorKind.Conflict, "user.email_taken", "An account already uses this email.");

    /// <summary>The Admin is the only account that manages the others, so it can't lock the office out.</summary>
    public static readonly Error CannotDeactivateSelf =
        new(ErrorKind.Conflict, "user.cannot_deactivate_self", "You can't deactivate your own account.");

    /// <summary>
    /// Your own password is changed with your current one (PUT /api/auth/password), so a session left open can't
    /// replace it without knowing it.
    /// </summary>
    public static readonly Error CannotSetOwnPassword =
        new(ErrorKind.Conflict, "user.cannot_set_own_password",
            "Change your own password from the account menu: it asks for your current one.");
}
