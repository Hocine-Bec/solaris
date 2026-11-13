namespace Infrastructure.Settings;

/// <summary>
/// The <c>Admin</c> section: the first Admin account, created at startup when no active Admin exists (FR-U3). There
/// are no default credentials in the code. Optional once an Admin exists; <see cref="AdminOptionsValidator"/> checks
/// the values when any is set. The password is a secret.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }

    /// <summary>Whether any of the three is set.</summary>
    public bool IsSet => !string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(Email) ||
                         !string.IsNullOrEmpty(Password);
}
