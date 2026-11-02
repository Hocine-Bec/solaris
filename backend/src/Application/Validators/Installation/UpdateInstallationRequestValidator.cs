using Application.DTOs.Installation;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Installation;

/// <summary>
/// Validator for UpdateInstallationRequest DTO
/// </summary>
public class UpdateInstallationRequestValidator : AbstractValidator<UpdateInstallationRequest>
{
    public UpdateInstallationRequestValidator()
    {
        // ProjectName validation (only if provided)
        RuleFor(x => x.ProjectName)
            .NotEmpty().WithMessage("Project name cannot be empty")
            .MaximumLength(255).WithMessage("Project name cannot exceed 255 characters")
            .MinimumLength(3).WithMessage("Project name must be at least 3 characters")
            .When(x => x.ProjectName != null);

        // Status validation (only if provided)
        RuleFor(x => x.Status)
            .Must(value => Enum.TryParse<InstallationStatus>(value, ignoreCase: true, out _))
                .WithMessage("Invalid installation status")
            .When(x => x.Status != null);

        // CompletionDate validation (only if provided)
        RuleFor(x => x.CompletionDate)
            .GreaterThanOrEqualTo(x => x.CompletionDate.Value)
                .WithMessage("Completion date cannot be in the past")
            .When(x => x.CompletionDate.HasValue);

        // SystemSizeKw validation (only if provided)
        RuleFor(x => x.SystemSizeKw)
            .GreaterThan(0).WithMessage("System size must be greater than 0 kW")
            .When(x => x.SystemSizeKw.HasValue);

        // PanelCount validation (only if provided)
        RuleFor(x => x.PanelCount)
            .GreaterThan(0).WithMessage("Panel count must be greater than 0")
            .When(x => x.PanelCount.HasValue);

        // InverterType validation (only if provided)
        RuleFor(x => x.InverterType)
            .MaximumLength(100).WithMessage("Inverter type cannot exceed 100 characters")
            .When(x => x.InverterType != null);

        // Notes validation (only if provided)
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes != null);
    }
}