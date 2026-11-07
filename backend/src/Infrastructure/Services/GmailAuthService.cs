using System.Text.Json;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class GmailAuthService(IOptions<EmailOptions> options) : IGmailAuthService
{
    private readonly GmailOptions _settings = options.Value.Gmail;
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
            return _cachedAccessToken;

        // Use lock to prevent multiple simultaneous refresh attempts
        await _refreshLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock (another thread might have refreshed)
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
                return _cachedAccessToken;

            await RefreshAccessTokenAsync();
            return _cachedAccessToken!;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task RefreshAccessTokenAsync()
    {
        var tokenRequest = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("refresh_token", _settings.RefreshToken),
            new KeyValuePair<string, string>("grant_type", "refresh_token")
        ]);
        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if(!response.IsSuccessStatusCode)
            throw new Exception($"Failed to refresh Gmail access token: {responseContent}");

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString() ?? string.Empty;
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        
        _cachedAccessToken = accessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 300);
    }
}