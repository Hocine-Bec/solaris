using Application.DTOs.User;
using FluentValidation;

namespace Application.Validators.User;

/// <summary>
/// Validator for UpdateUserRequest DTO
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        // Email validation (only if provided)
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => x.Email != null);

        // FirstName validation (only if provided)
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name cannot be empty")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters")
            .When(x => x.FirstName != null);

        // LastName validation (only if provided)
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name cannot be empty")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters")
            .When(x => x.LastName != null);

        // Specialization validation (only if provided)
        RuleFor(x => x.Specialization)
            .MaximumLength(100).WithMessage("Specialization cannot exceed 100 characters")
            .When(x => x.Specialization != null);

        // LicenseNumber validation (only if provided)
        RuleFor(x => x.LicenseNumber)
            .MaximumLength(100).WithMessage("License number cannot exceed 100 characters")
            .When(x => x.LicenseNumber != null);
    }
}