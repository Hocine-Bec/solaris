namespace Domain.Common;

/// <summary>
/// A day as the property lives it (BR-P4). An installation's readings belong to the local day in its own time zone,
/// so a day is 92, 96 or 100 quarter hours where the clocks change, and always 96 where they don't.
/// </summary>
public static class LocalDays
{
    /// <summary>
    /// The UTC window a local day covers: from its midnight to the next, each converted in the zone itself, so a
    /// day that gains or loses an hour is that much longer or shorter.
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtc) WindowUtc(DateOnly day, string timeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return (MidnightUtc(day, zone), MidnightUtc(day.AddDays(1), zone));
    }

    /// <summary>The local day a moment falls in, in this zone.</summary>
    public static DateOnly LocalDateOf(DateTime utc, string timeZoneId) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)));

    /// <summary>The last local day that has finished in this zone: the one before the day under way there.</summary>
    public static DateOnly LastDayEnded(string timeZoneId, DateTime nowUtc) =>
        LocalDateOf(nowUtc, timeZoneId).AddDays(-1);

    /// <summary>
    /// The last local day that has finished everywhere the fleet stands (FR-P2): the earliest of the zones' own last
    /// days, so the dashboard never shows a day that is still running out west.
    /// </summary>
    public static DateOnly LastDayEndedEverywhere(IEnumerable<string> timeZoneIds, DateTime nowUtc) =>
        timeZoneIds.Select(zone => LastDayEnded(zone, nowUtc)).Min();

    private static DateTime MidnightUtc(DateOnly day, TimeZoneInfo zone)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // US zones change at 2 a.m., so midnight always exists; a zone that skipped it would start the day at the
        // moment the clocks jumped.
        if (zone.IsInvalidTime(midnight))
            return TimeZoneInfo.ConvertTimeToUtc(midnight.AddHours(1), zone);
        return TimeZoneInfo.ConvertTimeToUtc(midnight, zone);
    }
}
