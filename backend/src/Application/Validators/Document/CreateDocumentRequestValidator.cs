using Application.DTOs.Document;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Document;

/// <summary>
/// Validator for CreateDocumentRequest DTO 
/// </summary>
public class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters");

        RuleFor(x => x.FilePath)
            .NotEmpty().WithMessage("File path is required")
            .MaximumLength(500).WithMessage("File path cannot exceed 500 characters");

        RuleFor(x => x.FileType)
            .NotEmpty().WithMessage("File type is required")
            .MaximumLength(50).WithMessage("File type cannot exceed 50 characters")
            .Matches(@"^\w+$").WithMessage("File type can only contain letters, digits, and underscores");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File size must be greater than 0 bytes")
            .LessThanOrEqualTo(50 * 1024 * 1024).WithMessage("File size cannot exceed 50MB");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Document type is required")
            .Must(value => Enum.TryParse<DocumentType>(value, ignoreCase: true, out _))
            .WithMessage("Invalid document type.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes != null);

        RuleFor(x => x.UploadedByUserId)
            .GreaterThan(0).WithMessage("Uploaded by user ID must be a valid positive integer");

        // At least one of CustomerId, InstallationId, or TicketId must be provided
        RuleFor(x => x)
            .Must(x => x.CustomerId.HasValue || x.InstallationId.HasValue || x.TicketId.HasValue)
            .WithMessage("At least one of Customer ID, Installation ID, or Ticket ID must be provided");
    }

    private bool BeAValidDocumentType(string documentType)
    {
        
        // Assuming valid document types based on common document categories
        var validTypes = new[] { "Contract", "Proposal", "Invoice", "Warranty", "Manual", "Photo", "Other" };
        return validTypes.Contains(documentType, StringComparer.OrdinalIgnoreCase);
    }
}