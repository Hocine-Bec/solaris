namespace Application.DTOs.Users;

/// <summary>A corrected name or a new email. The password and the sessions don't change.</summary>
public sealed class UpdateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
