using Application.DTOs.Equipment;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Equipment;

/// <summary>
/// Validator for UpdateEquipmentRequest DTO
/// </summary>
public class UpdateEquipmentRequestValidator : AbstractValidator<UpdateEquipmentRequest>
{
    public UpdateEquipmentRequestValidator()
    {
        // Status validation (only if provided)
        RuleFor(x => x.Status)
            .Must(value => Enum.TryParse<EquipmentStatus>(value, ignoreCase: true, out _))
            .When(x => x.Status != null);

        // InstallationId validation (only if provided)
        RuleFor(x => x.InstallationId)
            .GreaterThan(0).WithMessage("Installation ID must be a valid positive integer")
            .When(x => x.InstallationId.HasValue);

        // WarrantyExpiryDate validation (only if provided)
        RuleFor(x => x.WarrantyExpiryDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow).WithMessage("Warranty expiry date cannot be in the past")
            .When(x => x.WarrantyExpiryDate.HasValue);

        // Specifications validation (only if provided)
        RuleFor(x => x.Specifications)
            .MaximumLength(2000).WithMessage("Specifications cannot exceed 2000 characters")
            .When(x => x.Specifications != null);
    }
}