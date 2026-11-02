using Application.DTOs.EnergyProduction;
using FluentValidation;

namespace Application.Validators.EnergyProduction;

/// <summary>
/// Validator for UpdateEnergyProductionRequest DTO
/// </summary>
public class UpdateEnergyProductionRequestValidator : AbstractValidator<UpdateEnergyProductionRequest>
{
    public UpdateEnergyProductionRequestValidator()
    {
        // ActualProductionKwh validation (only if provided)
        RuleFor(x => x.ActualProductionKwh)
            .GreaterThanOrEqualTo(0).WithMessage("Actual production must be non-negative")
            .When(x => x.ActualProductionKwh.HasValue);

        // ExpectedProductionKwh validation (only if provided)
        RuleFor(x => x.ExpectedProductionKwh)
            .GreaterThanOrEqualTo(0).WithMessage("Expected production must be non-negative")
            .When(x => x.ExpectedProductionKwh.HasValue);

        // HealthStatus validation (only if provided)
        RuleFor(x => x.HealthStatus)
            .Must(BeAValidHealthStatus).WithMessage("Invalid health status")
            .MaximumLength(50).WithMessage("Health status cannot exceed 50 characters")
            .When(x => x.HealthStatus != null);

        // Notes validation (only if provided)
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => x.Notes != null);
    }

    private bool BeAValidHealthStatus(string healthStatus)
    {
        // Assuming valid health status values based on common system health statuses
        var validStatuses = new[] { "Good", "Fair", "Poor", "Critical" };
        return validStatuses.Contains(healthStatus, StringComparer.OrdinalIgnoreCase);
    }
}