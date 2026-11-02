using Application.DTOs.Equipment;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Equipment;

/// <summary>
/// Validator for CreateEquipmentRequest DTO 
/// </summary>
public class CreateEquipmentRequestValidator : AbstractValidator<CreateEquipmentRequest>
{
    public CreateEquipmentRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Equipment type is required")
            .Must(value => Enum.TryParse<EquipmentType>(value, ignoreCase: true, out _))
            .WithMessage("Invalid equipment type");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required")
            .MaximumLength(100).WithMessage("Model cannot exceed 100 characters");

        RuleFor(x => x.SerialNumber)
            .NotEmpty().WithMessage("Serial number is required")
            .MaximumLength(255).WithMessage("Serial number cannot exceed 255 characters");

        RuleFor(x => x.Manufacturer)
            .NotEmpty().WithMessage("Manufacturer is required")
            .MaximumLength(100).WithMessage("Manufacturer cannot exceed 100 characters");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Purchase date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Purchase date cannot be in the future");

        RuleFor(x => x.WarrantyExpiryDate)
            .GreaterThan(x => x.PurchaseDate).WithMessage("Warranty expiry date must be after purchase date")
            .When(x => x.WarrantyExpiryDate.HasValue);

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost must be non-negative");

        RuleFor(x => x.Specifications)
            .MaximumLength(2000).WithMessage("Specifications cannot exceed 2000 characters")
            .When(x => x.Specifications != null);
    }
}