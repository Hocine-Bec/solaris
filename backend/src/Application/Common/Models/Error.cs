namespace Application.Common.Models;

/// <summary>
/// An expected failure. Its kind decides the HTTP status (in <c>WebAPI</c>); its code is stable, so clients can rely
/// on it (for example <c>lead.not_found</c>); its message is for people.
/// </summary>
public sealed record Error(ErrorKind Kind, string Code, string Message);

/// <summary>
/// The kinds of expected failure a service returns. Malformed requests never reach a service: validation rejects
/// them first.
/// </summary>
public enum ErrorKind
{
    /// <summary>The resource doesn't exist.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state: a duplicate, a final state, or a change made meanwhile.</summary>
    Conflict
}
