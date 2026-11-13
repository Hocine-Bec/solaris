namespace Application.Interfaces.Services;

public interface IUserService
{
    /// <summary>
    /// Creates the first Admin from these settings when no active Admin exists (FR-U3). Returns whether it created one.
    /// The settings were checked at startup, so they follow the rules.
    /// </summary>
    Task<bool> EnsureFirstAdminAsync(string name, string email, string password);

    /// <summary>Whether an active Admin exists.</summary>
    Task<bool> AnyActiveAdminAsync();
}
