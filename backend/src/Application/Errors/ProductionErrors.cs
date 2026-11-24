using Application.Common.Models;

namespace Application.Errors;

public static class ProductionErrors
{
    /// <summary>Before Solaris existed; no system can have produced anything.</summary>
    public const int EarliestYear = 2000;

    /// <summary>Far enough ahead for any plan, and near enough to catch a typed year that is nonsense.</summary>
    public const int LatestYear = 2100;

    public static readonly Error NotAYear =
        new(ErrorKind.Invalid, "validation_failed", $"Year must be between {EarliestYear} and {LatestYear}.", "Year");

    public static readonly Error NotAMonth =
        new(ErrorKind.Invalid, "validation_failed", "Month must be between 1 and 12.", "Month");
}
