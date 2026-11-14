using System.Diagnostics;
using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Users;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

// An account read by ID is tracked, so saving writes only the columns changed here, and a sign-in saving at the same
// moment writes only its own (its time, and the hash when it replaces one made with weaker settings).
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

    public async Task<PagedResponse<UserResponse>> GetPageAsync(PageRequest request)
    {
        var (accounts, totalCount) = await users.GetPageAsync(request.Offset(), request.PageSize);
        return new PagedResponse<UserResponse>(accounts.Select(UserResponse.From).ToList(), request.Page,
            request.PageSize, totalCount);
    }

    public async Task<Result<UserResponse>> GetByIdAsync(int id)
    {
        var user = await users.GetByIdAsync(id);
        return user is null
            ? Result.Failure<UserResponse>(UserErrors.NotFound(id))
            : Result.Success(UserResponse.From(user));
    }

    public async Task<Result<UserResponse>> CreateTechnicianAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = User.NormalizeEmail(request.Email),
            PasswordHash = hasher.Hash(request.Password),
            Role = UserRole.Technician,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        users.Add(user);

        // The unique index on the normalized email decides, also between two requests at the same moment.
        return await unitOfWork.SaveAsync() switch
        {
            SaveOutcome.Saved => Result.Success(UserResponse.From(user)),
            SaveOutcome.Duplicate => Result.Failure<UserResponse>(UserErrors.EmailTaken),
            var other => throw new UnreachableException($"Adding an account can't end as {other}.")
        };
    }

    public async Task<Result> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(id));

        user.Name = request.Name.Trim();
        user.Email = request.Email.Trim();
        user.NormalizedEmail = User.NormalizeEmail(request.Email);
        return await SaveAsync(id);
    }

    public async Task<Result> SetPasswordAsync(int actingUserId, int id, SetPasswordRequest request)
    {
        if (id == actingUserId)
            return Result.Failure(UserErrors.CannotSetOwnPassword);

        var user = await users.GetByIdAsync(id);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(id));

        user.SetPassword(hasher.Hash(request.Password), DateTime.UtcNow);
        return await SaveAsync(id);
    }

    public async Task<Result> DeactivateAsync(int actingUserId, int id)
    {
        if (id == actingUserId)
            return Result.Failure(UserErrors.CannotDeactivateSelf);

        return await SetActiveAsync(id, false);
    }

    public Task<Result> ReactivateAsync(int id) => SetActiveAsync(id, true);

    // Already in that state is a success: nothing changes, and the screen shows what it asked for.
    private async Task<Result> SetActiveAsync(int id, bool isActive)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(id));

        user.IsActive = isActive;
        return await SaveAsync(id);
    }

    // Accounts are never deleted by the API; a conflict means the row went anyway, so it no longer exists.
    private async Task<Result> SaveAsync(int id) => await unitOfWork.SaveAsync() switch
    {
        SaveOutcome.Saved => Result.Success(),
        SaveOutcome.Duplicate => Result.Failure(UserErrors.EmailTaken),
        _ => Result.Failure(UserErrors.NotFound(id))
    };
}
