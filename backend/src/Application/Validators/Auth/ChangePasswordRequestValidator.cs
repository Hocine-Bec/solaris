using Application.DTOs.Auth;
using Application.Validators.Users;
using Domain.Common;
using FluentValidation;

namespace Application.Validators.Auth;

/// <summary>
/// The current password is only checked for being there, as at sign-in; whether it is right is the service's answer.
/// The new one follows the password rule.
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required")
            .MaximumLength(PasswordRule.MaxLength)
            .WithMessage($"Current password must be at most {PasswordRule.MaxLength} characters");

        RuleFor(x => x.NewPassword).FollowsPasswordRule();
    }
}
