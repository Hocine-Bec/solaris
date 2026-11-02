using Application.DTOs.InstallationStatusHistory;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.InstallationStatusHistory;

/// <summary>
/// Validator for CreateInstallationStatusHistoryRequest DTO 
/// </summary>
public class CreateInstallationStatusHistoryRequestValidator : AbstractValidator<CreateInstallationStatusHistoryRequest>
{
    public CreateInstallationStatusHistoryRequestValidator()
    {
        RuleFor(x => x.InstallationId)
            .GreaterThan(0).WithMessage("Installation ID must be a valid positive integer");

        RuleFor(x => x.ToStatus)
            .NotEmpty().WithMessage("Target status is required")
            .Must(value => Enum.TryParse<InstallationStatus>(value, ignoreCase: true, out _))
            .WithMessage("Invalid installation status");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => x.Notes != null);

        RuleFor(x => x.ChangedByUserId)
            .GreaterThan(0).WithMessage("Changed by user ID must be a valid positive integer");
    }
}