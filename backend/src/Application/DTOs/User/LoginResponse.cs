namespace Application.DTOs.User;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserResponse User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}