namespace Application.DTOs.Users;

/// <summary>A new technician's account (FR-U2): the Admin gives them this email and password in person.</summary>
public sealed class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
