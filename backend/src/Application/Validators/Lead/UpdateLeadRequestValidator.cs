using Application.DTOs.Lead;
using FluentValidation;

namespace Application.Validators.Lead;

/// <summary>An open lead's corrected contact details (FR-L7): the quote form's checks for the same fields.</summary>
public class UpdateLeadRequestValidator : AbstractValidator<UpdateLeadRequest>
{
    public UpdateLeadRequestValidator()
    {
        RuleFor(x => x.FirstName).IsLeadName("First name");
        RuleFor(x => x.LastName).IsLeadName("Last name");
        RuleFor(x => x.Email).IsLeadEmail();
        RuleFor(x => x.Phone).IsLeadPhone();
    }
}
