using Application.Interfaces.Repositories.Base;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserRepo : IGenericRepo<User>
{
    /// <summary>The account with this email, compared as <see cref="User.NormalizeEmail"/> does.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>Whether an active Admin exists.</summary>
    Task<bool> AnyActiveAdminAsync();
}
