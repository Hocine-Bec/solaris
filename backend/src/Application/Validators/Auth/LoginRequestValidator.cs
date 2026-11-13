using Application.DTOs.Auth;
using Domain.Common;
using FluentValidation;

namespace Application.Validators.Auth;

/// <summary>
/// Signing in checks only that both were given, and bounds their length: whether they are right is the service's
/// answer, the same for every refusal. The password rule applies when a password is set, not here.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(255).WithMessage("Email must be at most 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MaximumLength(PasswordRule.MaxLength)
            .WithMessage($"Password must be at most {PasswordRule.MaxLength} characters");
    }
}
