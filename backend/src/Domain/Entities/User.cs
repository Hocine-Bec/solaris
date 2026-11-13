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

    /// <summary>How an email is compared: trimmed and upper-cased, the same for every culture.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
