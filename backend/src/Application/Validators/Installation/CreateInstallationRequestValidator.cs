using Application.DTOs.Installation;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator for CreateInstallationRequest DTO 
/// </summary>
public class CreateInstallationRequestValidator : AbstractValidator<CreateInstallationRequest>
{
    public CreateInstallationRequestValidator()
    {
        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(255).WithMessage("Project name cannot exceed 255 characters")
            .MinimumLength(3).WithMessage("Project name must be at least 3 characters");

        RuleFor(x => x.SystemSizeKw)
            .GreaterThan(0).WithMessage("System size must be greater than 0 kW");

        RuleFor(x => x.PanelCount)
            .GreaterThan(0).WithMessage("Panel count must be greater than 0");

        RuleFor(x => x.InverterType)
            .MaximumLength(100).WithMessage("Inverter type cannot exceed 100 characters")
            .When(x => x.InverterType != null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes != null);

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be a valid positive integer");

        RuleFor(x => x.InstallationAddressId)
            .GreaterThan(0).WithMessage("Installation address ID must be a valid positive integer");
    }
}