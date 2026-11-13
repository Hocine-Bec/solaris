using Application.Common.Models;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class UserService(IUserRepo users, IPasswordHasher hasher, IUnitOfWork unitOfWork) : IUserService
{
    public async Task<bool> EnsureFirstAdminAsync(string name, string email, string password)
    {
        if (await users.AnyActiveAdminAsync())
            return false;

        users.Add(new User
        {
            Name = name.Trim(),
            Email = email.Trim(),
            NormalizedEmail = User.NormalizeEmail(email),
            PasswordHash = hasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        // A duplicate means another instance created it at the same moment, or a deactivated account has this email.
        return await unitOfWork.SaveAsync() == SaveOutcome.Saved;
    }

    public Task<bool> AnyActiveAdminAsync() => users.AnyActiveAdminAsync();
}
