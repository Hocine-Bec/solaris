using Application.DTOs.Lead;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Lead;

/// <summary>
/// The statuses a lead is moved to by name (BR-L2). Converted needs the conversion, which also creates the customer and
/// the installation; nothing goes back to New.
/// </summary>
public class UpdateLeadStatusRequestValidator : AbstractValidator<UpdateLeadStatusRequest>
{
    private static readonly string[] Allowed =
        [nameof(LeadStatus.Contacted), nameof(LeadStatus.Disqualified), nameof(LeadStatus.Lost)];

    public UpdateLeadStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required")
            .NotEqual(nameof(LeadStatus.Converted))
            .WithMessage("Convert the lead instead: converting also creates its customer and installation")
            .Must(status => Allowed.Contains(status))
            .WithMessage($"Status must be one of: {string.Join(", ", Allowed)}");
    }
}
