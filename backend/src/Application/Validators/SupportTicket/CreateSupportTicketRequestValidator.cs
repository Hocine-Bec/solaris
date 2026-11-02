using Application.DTOs.SupportTicket;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.SupportTicket;

/// <summary>
/// Validator for CreateSupportTicketRequest DTO 
/// </summary>
public class CreateSupportTicketRequestValidator : AbstractValidator<CreateSupportTicketRequest>
{
    public CreateSupportTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters")
            .MinimumLength(5).WithMessage("Title must be at least 5 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required")
            .Must(value => Enum.TryParse<TicketPriority>(value, ignoreCase: true, out _))
            .WithMessage("Invalid priority value");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID must be a valid positive integer");

        RuleFor(x => x.InstallationId)
            .GreaterThan(0).WithMessage("Installation ID must be a valid positive integer")
            .When(x => x.InstallationId.HasValue);
    }
}