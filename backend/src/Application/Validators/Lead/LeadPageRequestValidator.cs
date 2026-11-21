using Application.DTOs.Lead;
using Application.Validators.Common;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Lead;

public class LeadPageRequestValidator : AbstractValidator<LeadPageRequest>
{
    public LeadPageRequestValidator()
    {
        Include(new PageRequestValidator());

        RuleForEach(x => x.Status)
            .IsEnumName(typeof(LeadStatus), caseSensitive: true)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<LeadStatus>())}");
    }
}
