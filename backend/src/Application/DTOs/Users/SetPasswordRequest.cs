namespace Application.DTOs.Users;

/// <summary>A new password the Admin sets for someone else, which ends that account's sessions.</summary>
public sealed class SetPasswordRequest
{
    public string Password { get; set; } = string.Empty;
}
