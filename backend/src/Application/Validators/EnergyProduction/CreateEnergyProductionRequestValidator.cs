using Application.DTOs.EnergyProduction;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.EnergyProduction;

/// <summary>
/// Validator for CreateEnergyProductionRequest DTO 
/// </summary>
public class CreateEnergyProductionRequestValidator : AbstractValidator<CreateEnergyProductionRequest>
{
    public CreateEnergyProductionRequestValidator()
    {
        RuleFor(x => x.InstallationId)
            .GreaterThan(0).WithMessage("Installation ID must be a valid positive integer");

        RuleFor(x => x.ProductionDate)
            .NotEmpty().WithMessage("Production date is required")
            .NotEqual((DateOnly)default).WithMessage("Production date must be a valid date");

        RuleFor(x => x.ActualProductionKwh)
            .GreaterThanOrEqualTo(0).WithMessage("Actual production must be non-negative");

        RuleFor(x => x.ExpectedProductionKwh)
            .GreaterThanOrEqualTo(0).WithMessage("Expected production must be non-negative");

        RuleFor(x => x.HealthStatus)
            .NotEmpty().WithMessage("Health status is required")
            .Must(value => Enum.TryParse<SystemHealthStatus>(value, ignoreCase: true, out _))
            .WithMessage("Invalid health status");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => x.Notes != null);

        RuleFor(x => x.WeatherDataId)
            .GreaterThan(0).WithMessage("Weather data ID must be a valid positive integer")
            .When(x => x.WeatherDataId.HasValue);
    }
}