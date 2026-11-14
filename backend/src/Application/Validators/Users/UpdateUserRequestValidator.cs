using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators.Users;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name).IsAccountName();
        RuleFor(x => x.Email).IsAccountEmail();
    }
}
