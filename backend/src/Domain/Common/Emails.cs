namespace Domain.Common;

public static class Emails
{
    /// <summary>
    /// How an email is compared, for accounts, customers and leads alike: trimmed and upper-cased, the same for every
    /// culture, so two emails that differ only by case or surrounding spaces are one.
    /// </summary>
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
