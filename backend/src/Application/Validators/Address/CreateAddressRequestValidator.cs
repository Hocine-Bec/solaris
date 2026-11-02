using Application.DTOs.Address;
using FluentValidation;

namespace Application.Validators.Address;

/// <summary>
/// Validator for CreateAddressRequest DTO 
/// </summary>
public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required")
            .MaximumLength(255).WithMessage("Street cannot exceed 255 characters")
            .MinimumLength(3).WithMessage("Street must be at least 3 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("City must be at least 2 characters");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(100).WithMessage("State cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("State must be at least 2 characters");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZipCode is required")
            .MaximumLength(20).WithMessage("ZipCode cannot exceed 20 characters")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("ZipCode must be in format 12345 or 12345-6789");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Longitude.HasValue);

        // Custom rule: If latitude is provided, longitude should also be provided
        RuleFor(x => x.Longitude)
            .NotNull().WithMessage("Longitude is required when Latitude is provided")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Latitude)
            .NotNull().WithMessage("Latitude is required when Longitude is provided")
            .When(x => x.Longitude.HasValue);
    }
}