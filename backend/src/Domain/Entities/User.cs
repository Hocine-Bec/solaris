using Domain.Enums;

namespace Domain.Entities;

/// <summary>A staff account: an Admin or a Technician, signing in with an email and a password (FR-U1).</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>The name shown in the staff app: the account button, and later the assignments.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The email as it was entered.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The email as it is compared: two accounts can't differ only by case or surrounding spaces.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>The password, hashed with its salt and parameters; the password itself is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>A deactivated account can't sign in, and its open sessions stop at their next request (FR-U2).</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Sessions started before this moment are refused at their next request (ADR 7): a new password moves it to now,
    /// so a session started with the old one ends. Null until the account's password is first replaced.
    /// </summary>
    public DateTime? SessionsValidFrom { get; set; }

    /// <summary>
    /// Replaces the password and ends every session started before this second. Whole seconds, because a session
    /// token's issue time has none smaller: the session that changes its own password gets a new token in the same
    /// second, and keeps it. A session started earlier in that same second keeps going too.
    /// </summary>
    public void SetPassword(string passwordHash, DateTime now)
    {
        PasswordHash = passwordHash;
        SessionsValidFrom = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }

    /// <summary>How an email is compared: trimmed and upper-cased, the same for every culture.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
