namespace Domain.Common;

/// <summary>
/// The rule every password set for an account follows: at least 8 characters, with an uppercase letter, a number and
/// a special character (anything but a letter, a number or a space), and at most 128 characters, so a huge input can't
/// tie up the server while it is hashed. Signing in checks only that a password was given.
/// </summary>
public static class PasswordRule
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>What the password is missing, in the order a person would fix it; empty when it follows the rule.</summary>
    public static IReadOnlyList<string> Problems(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return ["Password is required"];

        var problems = new List<string>();
        if (password.Length < MinLength)
            problems.Add($"Password must be at least {MinLength} characters");
        if (password.Length > MaxLength)
            problems.Add($"Password must be at most {MaxLength} characters");
        if (!password.Any(char.IsUpper))
            problems.Add("Password must contain an uppercase letter");
        if (!password.Any(char.IsDigit))
            problems.Add("Password must contain a number");
        if (!password.Any(IsSpecial))
            problems.Add("Password must contain a special character, such as ! or #");
        return problems;
    }

    public static bool IsValid(string? password) => Problems(password).Count == 0;

    private static bool IsSpecial(char c) => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c);
}
