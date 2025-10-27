namespace Application.Interfaces.Services;

public interface IGmailAuthService
{
    Task<string> GetAccessTokenAsync();
}