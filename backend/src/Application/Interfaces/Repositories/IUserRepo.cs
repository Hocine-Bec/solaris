using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserRepo : IGenericRepo<User>
{
    /// <summary>The account with this email, compared as <see cref="User.NormalizeEmail"/> does.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>Whether an active Admin exists.</summary>
    Task<bool> AnyActiveAdminAsync();

    /// <summary>Whether the account with this ID exists and is active: every signed-in request asks (ADR 7).</summary>
    Task<bool> IsActiveAsync(int id);
}
