namespace Application.Common.Models;

/// <summary>
/// An expected failure. Its kind decides the HTTP status (in <c>WebAPI</c>); its code is stable, so clients can rely
/// on it (for example <c>lead.not_found</c>); its message is for people. An <see cref="ErrorKind.Invalid"/> error names
/// the request's field it is about, as validation does.
/// </summary>
public sealed record Error(ErrorKind Kind, string Code, string Message, string? Field = null);

/// <summary>
/// The kinds of expected failure a service returns. Malformed requests never reach a service: validation rejects
/// them first.
/// </summary>
public enum ErrorKind
{
    /// <summary>The resource doesn't exist.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state: a duplicate, a final state, or a change made meanwhile.</summary>
    Conflict,

    /// <summary>Who is asking isn't known, or no longer may: refused credentials, or an account deactivated since.</summary>
    Unauthorized,

    /// <summary>
    /// A field is well formed but wrong, which only the service can tell (a current password that doesn't match):
    /// answered like a validation failure, on that field.
    /// </summary>
    Invalid
}
