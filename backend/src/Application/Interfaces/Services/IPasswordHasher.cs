namespace Application.Interfaces.Services;

/// <summary>Hashes passwords and checks them against a stored hash.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordCheck Verify(string hash, string password);

    /// <summary>
    /// Takes as long as <see cref="Verify"/>, for an email with no account, so the time an answer takes doesn't tell
    /// whether the email has one.
    /// </summary>
    void VerifyNothing(string password);
}

public enum PasswordCheck
{
    Failed,
    Succeeded,

    /// <summary>The password is right, but its hash was made with weaker settings and should be replaced.</summary>
    SucceededRehashNeeded
}
