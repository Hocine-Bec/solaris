namespace Application.DTOs.Auth;

/// <summary>Your own new password, with the current one, so a session left open can't change it.</summary>
public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
