using Application.DTOs.Customer;
using FluentValidation;

namespace Application.Validators.Customer;

/// <summary>
/// Validator for UpdateCustomerRequest DTO
/// </summary>
public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
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

        // Email validation (only if provided)
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be empty")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => x.Email != null);

        // PhoneNumber validation (only if provided)
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number cannot be empty")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .Matches(@"^[\+]?[1-9][\d]{0,15}$").WithMessage("Phone number must be a valid format")
            .When(x => x.PhoneNumber != null);

        // Status validation (only if provided)
        RuleFor(x => x.Status)
            .Must(BeAValidStatus).WithMessage("Invalid status value")
            .When(x => x.Status != null);
    }

    private bool BeAValidStatus(string? status)
    {
        var validStatuses = new[] { "Lead", "Prospect", "Active", "Inactive" };
        return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}