using Application.DTOs.Lead;
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
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(100).WithMessage("State cannot exceed 100 characters");

        RuleFor(x => x.PropertyZipCode)
            .NotEmpty().WithMessage("Zip code is required")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Zip code must be in format 12345 or 12345-6789");

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