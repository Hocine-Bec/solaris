namespace Application.Common.Exceptions;

/// <summary>
/// Thrown by a repository when a save finds that the row was changed or deleted after it was read.
/// It keeps EF Core's own exception out of <c>Application</c> (ADR 1) until the unit of work returns
/// save outcomes instead (ADR 2).
/// </summary>
public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : Exception(message, innerException);
