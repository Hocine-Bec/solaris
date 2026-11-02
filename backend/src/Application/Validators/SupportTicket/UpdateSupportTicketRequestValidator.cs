using Application.DTOs.SupportTicket;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.SupportTicket;

/// <summary>
/// Validator for UpdateSupportTicketRequest DTO
/// </summary>
public class UpdateSupportTicketRequestValidator : AbstractValidator<UpdateSupportTicketRequest>
{
    public UpdateSupportTicketRequestValidator()
    {
        // Title validation (only if provided)
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title cannot be empty")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters")
            .When(x => x.Title != null);

        // Description validation (only if provided)
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description cannot be empty")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters")
            .When(x => x.Description != null);

        // Status validation (only if provided)
        RuleFor(x => x.Status)
            .Must(value => Enum.TryParse<TicketStatus>(value, ignoreCase: true, out _))
            .WithMessage("Invalid status value")
            .When(x => x.Status != null);

        // Priority validation (only if provided)
        RuleFor(x => x.Priority)
            .Must(value => Enum.TryParse<TicketPriority>(value, ignoreCase: true, out _))
            .WithMessage("Invalid priority value")
            .When(x => x.Priority != null);

        // AssignedToUserId validation (only if provided)
        RuleFor(x => x.AssignedToUserId)
            .GreaterThan(0).WithMessage("Assigned to user ID must be a valid positive integer")
            .When(x => x.AssignedToUserId.HasValue);

        // ResolutionNotes validation (only if provided)
        RuleFor(x => x.ResolutionNotes)
            .MaximumLength(5000).WithMessage("Resolution notes cannot exceed 5000 characters")
            .When(x => x.ResolutionNotes != null);
    }
}