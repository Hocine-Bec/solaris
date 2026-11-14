using System.Text.RegularExpressions;
using FluentValidation;

namespace Application.Validators.Common;

/// <summary>
/// Text that the welcome email repeats to an address nobody has confirmed can't hold anything an email app would show
/// as a link: a scheme (<c>://</c>), an <c>@</c>, or a dot followed by two letters, the end of a domain such as
/// <c>.com</c> (the full stops of other scripts count as dots). Otherwise the quote form could be used to send anyone
/// an email from Solaris carrying someone else's link. A dot followed by a space ("St. John") or by one letter
/// ("N.W.") is fine.
/// </summary>
public static partial class WebAddressRule
{
    [GeneratedRegex(@"://|@|[.。．｡]\p{L}{2}", RegexOptions.CultureInvariant)]
    private static partial Regex WebAddress();

    public static bool HasWebAddress(string? value) => value is not null && WebAddress().IsMatch(value);

    public static IRuleBuilderOptions<T, string> HasNoWebAddress<T>(this IRuleBuilder<T, string> rule, string field) =>
        rule.Must(value => !HasWebAddress(value)).WithMessage($"{field} can't contain a web or email address");
}
