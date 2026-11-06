using Application.DTOs.Lead;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Lead;

public class UpdateLeadStatusRequestValidator : AbstractValidator<UpdateLeadStatusRequest>
{
    public UpdateLeadStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .IsEnumName(typeof(LeadStatus), caseSensitive: true)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<LeadStatus>())}");
    }
}
