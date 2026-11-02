using Application.DTOs.Document;
using FluentValidation;

namespace Application.Validators.Document;

/// <summary>
/// Validator for UpdateDocumentRequest DTO
/// </summary>
public class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
{
    public UpdateDocumentRequestValidator()
    {
        // Description validation (only if provided)
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => x.Description != null);

        // Notes validation (only if provided)
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes != null);
    }
}