using Application.DTOs.Common;
using FluentValidation;

namespace Application.Validators.Common;

public class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1");

        // The number of items to skip must fit in an int, or the database query can't be built.
        RuleFor(x => x.Page)
            .Must((request, page) => (long)(page - 1) * request.PageSize <= int.MaxValue)
            .WithMessage("Page is too large for this page size")
            .When(x => x.Page >= 1 && x.PageSize is >= 1 and <= PageRequest.MaxPageSize);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {PageRequest.MaxPageSize}");
    }
}
