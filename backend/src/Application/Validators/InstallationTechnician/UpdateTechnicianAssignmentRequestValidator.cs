using Application.DTOs.InstallationTechnician;
using FluentValidation;

namespace Application.Validators.InstallationTechnician;

/// <summary>
/// Validator for UpdateTechnicianAssignmentRequest DTO
/// </summary>
public class UpdateTechnicianAssignmentRequestValidator : AbstractValidator<UpdateTechnicianAssignmentRequest>
{
    public UpdateTechnicianAssignmentRequestValidator()
    {
        // CompletedDate validation (only if provided)
        RuleFor(x => x.CompletedDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Completed date cannot be in the future")
            .When(x => x.CompletedDate.HasValue);

        // Role validation (only if provided)
        RuleFor(x => x.Role)
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters")
            .When(x => x.Role != null);

        // Notes validation (only if provided)
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => x.Notes != null);
    }
}