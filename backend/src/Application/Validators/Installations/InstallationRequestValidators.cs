using Application.DTOs.Installations;
using Application.Validators.Common;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Validators.Installations;

public class InstallationPageRequestValidator : AbstractValidator<InstallationPageRequest>
{
    public InstallationPageRequestValidator()
    {
        Include(new PageRequestValidator());
        RuleForEach(x => x.Status)
            .IsEnumName(typeof(InstallationStatus), caseSensitive: true)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<InstallationStatus>())}");
    }
}

/// <summary>A stage by name; Cancelled and Deactivated have their own requests, with a reason or a note.</summary>
public class MoveInstallationRequestValidator : AbstractValidator<MoveInstallationRequest>
{
    private static readonly string[] Stages =
    [
        nameof(InstallationStatus.Design), nameof(InstallationStatus.Permits), nameof(InstallationStatus.Installation),
        nameof(InstallationStatus.Inspection), nameof(InstallationStatus.Active)
    ];

    public MoveInstallationRequestValidator()
    {
        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required")
            .Must(s => s is not (nameof(InstallationStatus.Cancelled) or nameof(InstallationStatus.Deactivated)))
            .WithMessage("Cancel or deactivate the installation instead: each has its own request")
            .Must(s => Stages.Contains(s)).WithMessage($"Status must be one of: {string.Join(", ", Stages)}");
    }
}

public class CancelInstallationRequestValidator : AbstractValidator<CancelInstallationRequest>
{
    public CancelInstallationRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r)).WithMessage("Give a reason: it stays in the history")
            .MaximumLength(Installation.MaxNoteLength).WithMessage($"The reason cannot exceed {Installation.MaxNoteLength} characters");
    }
}

public class DeactivateInstallationRequestValidator : AbstractValidator<DeactivateInstallationRequest>
{
    public DeactivateInstallationRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(Installation.MaxNoteLength).WithMessage($"The note cannot exceed {Installation.MaxNoteLength} characters");
    }
}

/// <summary>The design's limits catch a typo (840 for 8.4), not a business rule.</summary>
public class InstallationDesignRequestValidator : AbstractValidator<InstallationDesignRequest>
{
    public InstallationDesignRequestValidator()
    {
        RuleFor(x => x.SystemSizeKw)
            .InclusiveBetween(Installation.MinSystemSizeKw, Installation.MaxSystemSizeKw)
            .WithMessage($"System size must be between {Installation.MinSystemSizeKw} and {Installation.MaxSystemSizeKw} kW")
            .PrecisionScale(5, 2, ignoreTrailingZeros: true).WithMessage("System size can have at most 2 decimals");
        RuleFor(x => x.PanelCount)
            .InclusiveBetween(Installation.MinPanelCount, Installation.MaxPanelCount)
            .WithMessage($"Panel count must be between {Installation.MinPanelCount} and {Installation.MaxPanelCount}");
        RuleFor(x => x.InverterType)
            .MaximumLength(Installation.MaxInverterTypeLength)
            .WithMessage($"Inverter type cannot exceed {Installation.MaxInverterTypeLength} characters")
            .HasNoWebAddress("Inverter type");
    }
}

public class InstallationTimeZoneRequestValidator : AbstractValidator<InstallationTimeZoneRequest>
{
    public InstallationTimeZoneRequestValidator()
    {
        RuleFor(x => x.TimeZone)
            .Must(z => UsTimeZones.All.Any(u => u.Id == z))
            .WithMessage($"Time zone must be one of: {string.Join(", ", UsTimeZones.All.Select(z => z.Id))}");
    }
}

public class AssignTechnicianRequestValidator : AbstractValidator<AssignTechnicianRequest>
{
    public AssignTechnicianRequestValidator()
    {
        RuleFor(x => x.TechnicianId).GreaterThan(0).WithMessage("Choose a technician");
        RuleFor(x => x.Role)
            .IsEnumName(typeof(AssignmentRole), caseSensitive: true)
            .WithMessage($"Role must be one of: {string.Join(", ", Enum.GetNames<AssignmentRole>())}");
    }
}
