using Application.DTOs.Lead;
using Domain.Common;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Lead;

public class CreateLeadRequestValidator : AbstractValidator<CreateLeadRequest>
{
    public CreateLeadRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Invalid phone number format");

        RuleFor(x => x.PropertyStreet)
            .NotEmpty().WithMessage("Property street is required")
            .MaximumLength(255).WithMessage("Street cannot exceed 255 characters");

        RuleFor(x => x.PropertyCity)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.PropertyState)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("State is required")
            .Must(UsStates.IsCode).WithMessage("State must be the two-letter code of a US state or DC, such as TX");

        RuleFor(x => x.PropertyZipCode)
            .NotEmpty().WithMessage("Zip code is required")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Zip code must be in format 12345 or 12345-6789");

        // Coordinates are optional, but come as a pair
        RuleFor(x => x.PropertyLatitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.PropertyLatitude.HasValue);

        RuleFor(x => x.PropertyLongitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.PropertyLongitude.HasValue);

        RuleFor(x => x.PropertyLongitude)
            .NotNull().WithMessage("Longitude is required when Latitude is provided")
            .When(x => x.PropertyLatitude.HasValue);

        RuleFor(x => x.PropertyLatitude)
            .NotNull().WithMessage("Latitude is required when Longitude is provided")
            .When(x => x.PropertyLongitude.HasValue);

        RuleFor(x => x.PropertyType)
            .NotEmpty().WithMessage("Property type is required")
            .Must(value => Enum.TryParse<PropertyType>(value, ignoreCase: true, out _))
            .WithMessage("Property type must be House, Apartment, or Commercial");

        RuleFor(x => x.IsPropertyOwner)
            .Equal(true).WithMessage("Solar installation is only available for property owners")
            .When(x => x.PropertyType != "Commercial");

        RuleFor(x => x.MonthlyBillRange)
            .NotEmpty().WithMessage("Monthly bill range is required");

        RuleFor(x => x.BestTimeToContact)
            .NotEmpty().WithMessage("Best time to contact is required")
            .Must(x => new[] { "Morning", "Afternoon", "Evening" }.Contains(x))
            .WithMessage("Best time must be Morning, Afternoon, or Evening");
    }
}