using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Users;

namespace Application.Interfaces.Services;

/// <summary>Staff accounts (FR-U2, FR-U3, FR-U4): the first Admin at startup, then the Admin's Users screens.</summary>
public interface IUserService
{
    /// <summary>
    /// Creates the first Admin from these settings when no active Admin exists (FR-U3). Returns whether it created one.
    /// The settings were checked at startup, so they follow the rules.
    /// </summary>
    Task<bool> EnsureFirstAdminAsync(string name, string email, string password);

    /// <summary>Whether an active Admin exists.</summary>
    Task<bool> AnyActiveAdminAsync();

    /// <summary>One page of accounts: active ones first, then by name.</summary>
    Task<PagedResponse<UserResponse>> GetPageAsync(PageRequest request);

    Task<Result<UserResponse>> GetByIdAsync(int id);

    /// <summary>A new technician's account, active, with the password the Admin gives them (FR-U2).</summary>
    Task<Result<UserResponse>> CreateTechnicianAsync(CreateUserRequest request);

    /// <summary>A corrected name or a new email; the password and sessions stay.</summary>
    Task<Result> UpdateAsync(int id, UpdateUserRequest request);

    /// <summary>
    /// A new password for someone else's account, ending its sessions. <paramref name="actingUserId"/> is who asks:
    /// their own password is changed with the current one instead.
    /// </summary>
    Task<Result> SetPasswordAsync(int actingUserId, int id, SetPasswordRequest request);

    /// <summary>The account can't sign in, and its sessions end at their next request (FR-U2). Not your own.</summary>
    Task<Result> DeactivateAsync(int actingUserId, int id);

    /// <summary>The account can sign in again, with its password.</summary>
    Task<Result> ReactivateAsync(int id);
}
