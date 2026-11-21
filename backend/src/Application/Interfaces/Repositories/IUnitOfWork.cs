using Application.Common.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Saves the changes made through the repositories in one database transaction. Each service operation
/// saves once, so its writes succeed or fail together (ADR 2).
/// </summary>
public interface IUnitOfWork
{
    Task<SaveOutcome> SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a transaction for an operation whose repositories write before the save (an insert that can't be
    /// duplicated, such as an address or a customer, ADR 5): those writes and the save then commit together, or not at
    /// all. Disposing it uncommitted rolls everything back.
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>A transaction started by <see cref="IUnitOfWork.BeginTransactionAsync"/>.</summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
