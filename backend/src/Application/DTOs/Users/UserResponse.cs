using Domain.Entities;

namespace Application.DTOs.Users;

/// <summary>A staff account as the Users screens show it: in the list, and on its own page. Never its password.</summary>
public sealed record UserResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt, user.LastLoginAt);
}
