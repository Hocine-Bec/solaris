using Domain.Enums;

namespace Domain.Common;

/// <summary>
/// What a day should have produced and how the day went (BR-P1, BR-P2).
///
/// A day's ratio is what the system made divided by what a system its size should make in that month where it
/// stands. Health is that ratio against <b>the fleet's</b> for the same day, not against the table alone: the
/// weather moves every system together, so a cloudy week would otherwise read as a fleet of broken systems
/// (ADR 15). When too few systems reported that day for a median to mean anything, the ratio stands on its own.
/// </summary>
public static class Production
{
    /// <summary>Of what a perfect system would make, what a real one does: wiring, inverter, dust, heat (BR-P1).</summary>
    public const decimal PerformanceRatio = 0.80m;

    /// <summary>Below this many systems reporting, a day's median says more about the few than about the weather.</summary>
    public const int SmallestFleet = 10;

    /// <summary>What a system of this size should make on a day of this month, where it stands (BR-P1).</summary>
    public static decimal ExpectedKwh(decimal systemSizeKw, string state, int month) =>
        systemSizeKw * PeakSunHours.For(state, month) * PerformanceRatio;

    /// <summary>
    /// The middle of the day's ratios, which the fleet's weather moves: the average of the two middle ones when
    /// there is an even number. Null when too few systems reported, and the day is then judged against the table
    /// alone. Only systems that reported count: a silent one is Offline, and would otherwise drag the middle down.
    /// </summary>
    public static decimal? FleetMedianRatio(IReadOnlyCollection<decimal> ratiosOfSystemsThatReported)
    {
        if (ratiosOfSystemsThatReported.Count < SmallestFleet)
            return null;
        var sorted = ratiosOfSystemsThatReported.Order().ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2m;
    }

    /// <summary>
    /// How a day went (BR-P2). <paramref name="actualKwh"/> is null when the day has no readings at all, which is
    /// Offline whatever the fleet did. <paramref name="fleetMedianRatio"/> is the day's middle ratio, or null when
    /// too few systems reported: then the ratio is read as it is, as it was before ADR 15.
    /// </summary>
    public static ProductionHealth HealthOf(decimal? actualKwh, decimal expectedKwh, decimal? fleetMedianRatio)
    {
        if (actualKwh is not { } actual)
            return ProductionHealth.Offline;
        // A system with no expected production at all can only be judged as having met it
        if (expectedKwh <= 0)
            return ProductionHealth.Excellent;

        var ratio = actual / expectedKwh;
        // Against the fleet when there is one: on a dark day its middle is low too, and a system that kept pace
        // with its neighbours is healthy even though it made little.
        var measured = fleetMedianRatio is { } median && median > 0 ? ratio / median : ratio;
        return measured switch
        {
            >= 0.90m => ProductionHealth.Excellent,
            >= 0.80m => ProductionHealth.Good,
            >= 0.60m => ProductionHealth.Warning,
            _ => ProductionHealth.Critical
        };
    }
}
