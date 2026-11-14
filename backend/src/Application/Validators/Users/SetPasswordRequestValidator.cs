using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users;

public class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
    {
        RuleFor(x => x.Password).FollowsPasswordRule();
    }
}
