namespace Infrastructure.Services;

/// <summary>Gets the OAuth2 access token the Gmail SMTP login needs, refreshing it when it expires.</summary>
public interface IGmailAuthService
{
    Task<string> GetAccessTokenAsync();
}
