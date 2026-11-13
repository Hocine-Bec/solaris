using Application.Common.Models;

namespace Application.Errors;

public static class AuthErrors
{
    /// <summary>
    /// Every refused sign-in: an unknown email, a wrong password or a deactivated account. One message, so a stranger
    /// can't learn which emails have an account (OWASP Authentication Cheat Sheet).
    /// </summary>
    public static readonly Error InvalidCredentials =
        new(ErrorKind.Unauthorized, "auth.invalid_credentials", "The email or password is incorrect.");

    /// <summary>A valid token whose account no longer exists or was deactivated.</summary>
    public static readonly Error SignedOut =
        new(ErrorKind.Unauthorized, "auth.signed_out", "Your session has ended. Sign in again.");
}
