using System.ComponentModel.DataAnnotations;
using Domain.Common;
using FluentValidation;

namespace Application.Validators.Users;

/// <summary>
/// The checks every account's fields get, whoever sets them: the same as the first Admin's settings at startup
/// (<c>AdminOptionsValidator</c>), compared after trimming, as they are stored.
/// </summary>
public static class AccountRules
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 255;

    public static IRuleBuilderOptions<T, string> IsAccountName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name is required")
            .Must(name => name is null || name.Trim().Length <= NameMaxLength)
            .WithMessage($"Name must be at most {NameMaxLength} characters");

    public static IRuleBuilderOptions<T, string> IsAccountEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(email => !string.IsNullOrWhiteSpace(email)).WithMessage("Email is required")
            .Must(email => email is null || string.IsNullOrWhiteSpace(email) ||
                           (new EmailAddressAttribute().IsValid(email.Trim()) && !email.Trim().Contains(' ')))
            .WithMessage("Email must be an email address, such as name@example.com")
            .Must(email => email is null || email.Trim().Length <= EmailMaxLength)
            .WithMessage($"Email must be at most {EmailMaxLength} characters");

    /// <summary>The password rule (<see cref="PasswordRule"/>): each thing the password is missing, on its field.</summary>
    public static IRuleBuilderOptionsConditions<T, string> FollowsPasswordRule<T>(this IRuleBuilder<T, string> rule) =>
        rule.Custom((password, context) =>
        {
            foreach (var problem in PasswordRule.Problems(password))
                context.AddFailure(problem);
        });
}
