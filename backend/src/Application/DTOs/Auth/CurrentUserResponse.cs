using Domain.Entities;

namespace Application.DTOs.Auth;

/// <summary>Who is signed in, as the staff app shows it: the account button and its menu.</summary>
public sealed record CurrentUserResponse(string Name, string Email, string Role)
{
    public static CurrentUserResponse From(User user) => new(user.Name, user.Email, user.Role.ToString());
}

/// <summary>A successful sign-in: the account, and the session token the API puts in its cookie.</summary>
public sealed record SignedIn(CurrentUserResponse User, string Token, DateTimeOffset ExpiresAt);
