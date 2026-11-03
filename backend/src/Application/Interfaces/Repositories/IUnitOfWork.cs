using Application.Common.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Saves the changes made through the repositories in one database transaction. Each service operation
/// saves once, so its writes succeed or fail together (ADR 2).
/// </summary>
public interface IUnitOfWork
{
    Task<SaveOutcome> SaveAsync(CancellationToken cancellationToken = default);
}
