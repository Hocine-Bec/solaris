using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserRepo : IGenericRepo<User>
{
    /// <summary>The account with this email, compared as <see cref="User.NormalizeEmail"/> does.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>One page of accounts, active ones first, then by name and ID, and the number of accounts.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPageAsync(int skip, int take);

    /// <summary>Whether an active Admin exists.</summary>
    Task<bool> AnyActiveAdminAsync();

    /// <summary>
    /// Whether a session of this account, started at <paramref name="issuedAt"/>, may go on: the account exists, is
    /// active, and its password hasn't been replaced since. Every signed-in request asks (ADR 7).
    /// </summary>
    Task<bool> AcceptsSessionAsync(int id, DateTime issuedAt);
}
