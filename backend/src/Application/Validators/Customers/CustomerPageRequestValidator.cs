using Application.DTOs.Customers;
using Application.Validators.Common;
using FluentValidation;

namespace Application.Validators.Customers;

public class CustomerPageRequestValidator : AbstractValidator<CustomerPageRequest>
{
    public CustomerPageRequestValidator()
    {
        Include(new PageRequestValidator());

        RuleFor(x => x.Q)
            .MaximumLength(CustomerPageRequest.MaxSearchLength)
            .WithMessage($"Search cannot exceed {CustomerPageRequest.MaxSearchLength} characters");
    }
}
