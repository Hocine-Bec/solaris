using Application.Common.Models;
using Application.DTOs.Auth;

namespace Application.Interfaces.Services;

public interface IAuthService
{
    /// <summary>Checks the email and password; on success, records the sign-in and issues a session token (FR-U1).</summary>
    Task<Result<SignedIn>> SignInAsync(LoginRequest request);

    /// <summary>The account a session belongs to, if it still exists and is active.</summary>
    Task<Result<CurrentUserResponse>> GetCurrentUserAsync(int userId);
}
