using Application.DTOs.WeatherData;
using FluentValidation;

namespace Application.Validators.WeatherData;

/// <summary>
/// Validator for CreateWeatherDataRequest DTO 
/// </summary>
public class CreateWeatherDataRequestValidator : AbstractValidator<CreateWeatherDataRequest>
{
    public CreateWeatherDataRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required")
            .MaximumLength(255).WithMessage("Location cannot exceed 255 characters")
            .MinimumLength(3).WithMessage("Location must be at least 3 characters");

        RuleFor(x => x.Condition)
            .NotEmpty().WithMessage("Condition is required")
            .MaximumLength(100).WithMessage("Condition cannot exceed 100 characters");

        RuleFor(x => x.TemperatureCelsius)
            .InclusiveBetween(-100, 100).WithMessage("Temperature must be between -100°C and 100°C");

        RuleFor(x => x.CloudCoverPercentage)
            .GreaterThanOrEqualTo(0).WithMessage("Cloud cover percentage must be between 0% and 100%")
            .LessThanOrEqualTo(100).WithMessage("Cloud cover percentage must be between 0% and 100%")
            .When(x => x.CloudCoverPercentage.HasValue);
    }
}