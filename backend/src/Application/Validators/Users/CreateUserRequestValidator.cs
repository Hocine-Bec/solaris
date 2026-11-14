using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).IsAccountName();
        RuleFor(x => x.Email).IsAccountEmail();
        RuleFor(x => x.Password).FollowsPasswordRule();
    }
}
