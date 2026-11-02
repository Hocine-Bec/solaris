using Application.DTOs.Address;
using FluentValidation;

namespace Application.Validators.Address;

/// <summary>
/// Validator for UpdateAddressRequest DTO
/// </summary>
public class UpdateAddressRequestValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        // Street validation (only if provided)
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street cannot be empty")
            .MaximumLength(255).WithMessage("Street cannot exceed 255 characters")
            .MinimumLength(3).WithMessage("Street must be at least 3 characters")
            .When(x => x.Street != null);

        // City validation (only if provided)
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City cannot be empty")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("City must be at least 2 characters")
            .When(x => x.City != null);

        // State validation (only if provided)
        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State cannot be empty")
            .MaximumLength(100).WithMessage("State cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("State must be at least 2 characters")
            .When(x => x.State != null);

        // ZipCode validation (only if provided)
        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZipCode cannot be empty")
            .MaximumLength(20).WithMessage("ZipCode cannot exceed 20 characters")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("ZipCode must be in format 12345 or 12345-6789")
            .When(x => x.ZipCode != null );

        // Country validation (only if provided)
        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country cannot be empty")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters")
            .When(x => x.Country != null);

        // Latitude validation (only if provided)
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Latitude.HasValue);

        // Longitude validation (only if provided)
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Longitude.HasValue);
    }
}