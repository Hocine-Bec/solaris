using Domain.Common;

namespace Solaris.DataGen;

/// <summary>How well one system runs, which decides what share of its expected production it reaches.</summary>
public enum SystemCondition
{
    /// <summary>Nine systems in ten: doing what it should.</summary>
    Healthy,

    /// <summary>Seven in a hundred: dirty, shaded or ageing.</summary>
    Degraded,

    /// <summary>Three in a hundred: a string down, an inverter failing.</summary>
    Failing
}

/// <summary>
/// The shape of a real day's production, for data that looks like a fleet rather than like a test fixture: a bell
/// around the property's own local noon, wider in summer; a cloudy day cuts the whole day down; and a few systems
/// are not well. The numbers are invented, not measured, and are only here to make the measurements realistic.
/// </summary>
public static class ProductionShape
{
    /// <summary>How much of its expected production a system in this condition reaches on a clear day.</summary>
    public static (decimal Low, decimal High) Reaches(SystemCondition condition) => condition switch
    {
        SystemCondition.Healthy => (0.95m, 1.00m),
        SystemCondition.Degraded => (0.60m, 0.80m),
        _ => (0.10m, 0.40m)
    };

    /// <summary>A system's condition, drawn so that 90% are healthy, 7% degraded and 3% failing.</summary>
    public static SystemCondition ConditionOf(double draw) => draw switch
    {
        < 0.90 => SystemCondition.Healthy,
        < 0.97 => SystemCondition.Degraded,
        _ => SystemCondition.Failing
    };

    /// <summary>
    /// How long the sun is up, in hours: 9 at midwinter to 15 at midsummer, a plain cosine over the year. Latitude
    /// would change it, but the peak sun hours per state already carry how much sun a place gets (BR-P1); this only
    /// spreads a day's energy over a believable number of slots.
    /// </summary>
    public static double DaylightHours(DateOnly day)
    {
        const double shortest = 9, longest = 15;
        var middle = (shortest + longest) / 2;
        var swing = (longest - shortest) / 2;
        // Day 172 is around the longest day of the year
        return middle + swing * Math.Cos(2 * Math.PI * (day.DayOfYear - 172) / 365.0);
    }

    /// <summary>
    /// How a day's energy is spread over its quarter hours: nothing before sunrise or after sunset, a cosine bell
    /// peaking at local noon. The weights add up to 1, so a day's total lands wherever it is wanted.
    /// </summary>
    public static double[] SlotWeights(int slotsInDay, double daylightHours)
    {
        var weights = new double[slotsInDay];
        var noon = slotsInDay / 2.0;
        var halfDaylight = daylightHours * 2;   // in quarter hours
        var total = 0.0;
        for (var slot = 0; slot < slotsInDay; slot++)
        {
            // The middle of the slot, as a distance from noon in quarter hours
            var fromNoon = Math.Abs(slot + 0.5 - noon);
            if (fromNoon >= halfDaylight)
                continue;
            // cos² over the daylight, so it rises and falls smoothly and is flat-topped at noon
            var shape = Math.Cos(Math.PI * fromNoon / (2 * halfDaylight));
            weights[slot] = shape * shape;
            total += weights[slot];
        }
        if (total <= 0)
            return weights;
        for (var slot = 0; slot < slotsInDay; slot++)
            weights[slot] /= total;
        return weights;
    }
}
