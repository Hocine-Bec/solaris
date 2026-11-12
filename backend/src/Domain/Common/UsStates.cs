namespace Domain.Common;

/// <summary>
/// Where Solaris works: the 50 states and the District of Columbia, by their two-letter postal codes (FR-L1).
/// </summary>
public static class UsStates
{
    public static readonly IReadOnlySet<string> Codes = new HashSet<string>(StringComparer.Ordinal)
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS",
        "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC",
        "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
    };

    /// <summary>True for one of the codes, in capitals ("TX", not "tx" or "Texas").</summary>
    public static bool IsCode(string? value) => value is not null && Codes.Contains(value);
}
