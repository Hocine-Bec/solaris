using Application.DTOs.Telemetry;
using FluentValidation;

namespace Application.Validators.Telemetry;

/// <summary>
/// The shape of a batch (FR-T1): at least one reading, at most 500. Whether a reading itself is any good is BR-T1's,
/// and is answered per reading in <c>rejected</c> rather than refusing the whole batch.
/// </summary>
public class UploadReadingsRequestValidator : AbstractValidator<UploadReadingsRequest>
{
    public UploadReadingsRequestValidator()
    {
        RuleFor(x => x.Readings)
            .NotEmpty().WithMessage("Send at least one reading")
            .Must(r => r.Count <= UploadReadingsRequest.MaxReadings)
            .WithMessage($"A batch holds at most {UploadReadingsRequest.MaxReadings} readings");
    }
}
