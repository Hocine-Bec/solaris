using Application.Common.Models;
using Application.DTOs.Auth;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services;

public class AuthService(IUserRepo users, IPasswordHasher hasher, ISessionTokenIssuer tokens, IUnitOfWork unitOfWork)
    : IAuthService
{
    public async Task<Result<SignedIn>> SignInAsync(LoginRequest request)
    {
        var user = await users.GetByEmailAsync(request.Email);
        if (user is null)
        {
            hasher.VerifyNothing(request.Password);
            return Result.Failure<SignedIn>(AuthErrors.InvalidCredentials);
        }

        // The password is checked even for a deactivated account, so its answer takes the same time as the others.
        var check = hasher.Verify(user.PasswordHash, request.Password);
        if (check == PasswordCheck.Failed || !user.IsActive)
            return Result.Failure<SignedIn>(AuthErrors.InvalidCredentials);

        // A hash made with weaker settings is replaced now, while the password is known (OWASP: rehash at sign-in).
        // The account was read tracked, so only these columns are written: a deactivation saved while the password
        // was being checked stays, and so does a new password, unless this sign-in also replaces the hash.
        if (check == PasswordCheck.SucceededRehashNeeded)
            user.PasswordHash = hasher.Hash(request.Password);
        user.LastLoginAt = DateTime.UtcNow;

        // Not saved means the account was deleted meanwhile: there is nobody to sign in.
        if (await unitOfWork.SaveAsync() != SaveOutcome.Saved)
            return Result.Failure<SignedIn>(AuthErrors.InvalidCredentials);

        var (token, expiresAt) = tokens.Issue(user);
        return Result.Success(new SignedIn(CurrentUserResponse.From(user), token, expiresAt));
    }

    public async Task<Result<CurrentUserResponse>> GetCurrentUserAsync(int userId)
    {
        // Read from the account each time, so a deactivated account stops at its next request.
        var user = await users.GetByIdAsync(userId);
        return user is { IsActive: true }
            ? Result.Success(CurrentUserResponse.From(user))
            : Result.Failure<CurrentUserResponse>(AuthErrors.SignedOut);
    }

    public async Task<Result<SignedIn>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await users.GetByIdAsync(userId);
        if (user is not { IsActive: true })
            return Result.Failure<SignedIn>(AuthErrors.SignedOut);

        if (hasher.Verify(user.PasswordHash, request.CurrentPassword) == PasswordCheck.Failed)
            return Result.Failure<SignedIn>(AuthErrors.CurrentPasswordIncorrect);

        // Every session started before now ends; this one goes on with a token issued after the change.
        user.SetPassword(hasher.Hash(request.NewPassword), DateTime.UtcNow);
        if (await unitOfWork.SaveAsync() != SaveOutcome.Saved)
            return Result.Failure<SignedIn>(AuthErrors.SignedOut);

        var (token, expiresAt) = tokens.Issue(user);
        return Result.Success(new SignedIn(CurrentUserResponse.From(user), token, expiresAt));
    }
}
