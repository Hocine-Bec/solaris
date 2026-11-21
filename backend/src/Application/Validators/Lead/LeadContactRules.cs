using Application.Validators.Common;
using FluentValidation;

namespace Application.Validators.Lead;

/// <summary>
/// The checks a lead's contact details get, sent with the quote form or corrected by the Admin (FR-L7). The names can't
/// hold a web or email address, as the welcome email repeats them (<see cref="WebAddressRule"/>).
/// </summary>
public static class LeadContactRules
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 255;
    public const int PhoneMaxLength = 20;

    public static IRuleBuilderOptions<T, string> IsLeadName<T>(this IRuleBuilder<T, string> rule, string label) =>
        rule.NotEmpty().WithMessage($"{label} is required")
            .MaximumLength(NameMaxLength).WithMessage($"{label} cannot exceed {NameMaxLength} characters")
            .MinimumLength(2).WithMessage($"{label} must be at least 2 characters")
            .HasNoWebAddress(label);

    public static IRuleBuilderOptions<T, string> IsLeadEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(EmailMaxLength).WithMessage($"Email cannot exceed {EmailMaxLength} characters");

    public static IRuleBuilderOptions<T, string> IsLeadPhone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Invalid phone number format")
            .MaximumLength(PhoneMaxLength).WithMessage($"Phone number cannot exceed {PhoneMaxLength} characters");
}
