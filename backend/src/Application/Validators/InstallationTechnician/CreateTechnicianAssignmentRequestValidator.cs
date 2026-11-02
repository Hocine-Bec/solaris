using Application.DTOs.InstallationTechnician;
using FluentValidation;

namespace Application.Validators.InstallationTechnician;

/// <summary>
/// Validator for CreateTechnicianAssignmentRequest DTO 
/// </summary>
public class CreateTechnicianAssignmentRequestValidator : AbstractValidator<CreateTechnicianAssignmentRequest>
{
    public CreateTechnicianAssignmentRequestValidator()
    {
        RuleFor(x => x.InstallationId)
            .GreaterThan(0).WithMessage("Installation ID must be a valid positive integer");

        RuleFor(x => x.TechnicianId)
            .GreaterThan(0).WithMessage("Technician ID must be a valid positive integer");

        RuleFor(x => x.Role)
            .MaximumLength(100).WithMessage("Role cannot exceed 100 characters")
            .When(x => x.Role != null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => x.Notes != null);
    }
}