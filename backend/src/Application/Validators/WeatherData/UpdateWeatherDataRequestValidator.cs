using Application.DTOs.WeatherData;
using FluentValidation;

namespace Application.Validators.WeatherData;

/// <summary>
/// Validator for UpdateWeatherDataRequest DTO
/// </summary>
public class UpdateWeatherDataRequestValidator : AbstractValidator<UpdateWeatherDataRequest>
{
    public UpdateWeatherDataRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .NotEqual(default(DateOnly))
            .When(x => x.Date.HasValue);
            
        RuleFor(x => x.Condition)
            .NotEmpty().WithMessage("Condition cannot be empty")
            .MaximumLength(100).WithMessage("Condition cannot exceed 100 characters")
            .When(x => x.Condition != null);

        // Temperature validation (only if provided)
        RuleFor(x => x.TemperatureCelsius)
            .InclusiveBetween(-100, 100).WithMessage("Temperature must be between -100°C and 100°C")
            .When(x => x.TemperatureCelsius.HasValue);

        // CloudCoverPercentage validation (only if provided)
        RuleFor(x => x.CloudCoverPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("Cloud cover percentage must be between 0% and 100%")
            .LessThanOrEqualTo(100).WithMessage("Cloud cover percentage must be between 0% and 100%")
            .When(x => x.CloudCoverPercentage.HasValue);
    }
}