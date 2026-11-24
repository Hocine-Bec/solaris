using FluentAssertions;
using Solaris.DataGen;

namespace UnitTests.DataGen;

// The shape the generated data takes, so a measurement runs against something that looks like a fleet.
public class ProductionShapeTests
{
    [Fact]
    public void ADaysEnergy_IsSpreadOverItsDaylightAndAddsUpToAllOfIt()
    {
        var weights = ProductionShape.SlotWeights(96, daylightHours: 12);

        weights.Sum().Should().BeApproximately(1.0, 1e-9);
        weights.Should().OnlyContain(w => w >= 0);
    }

    [Fact]
    public void NothingIsProducedBeforeSunriseOrAfterSunset()
    {
        // 12 hours of daylight in a 24-hour day: midnight to 06:00 and 18:00 to midnight are dark
        var weights = ProductionShape.SlotWeights(96, daylightHours: 12);

        weights.Take(24).Should().AllSatisfy(w => w.Should().Be(0));
        weights.TakeLast(24).Should().AllSatisfy(w => w.Should().Be(0));
        weights.Skip(24).Take(48).Should().OnlyContain(w => w > 0);
    }

    [Fact]
    public void TheMostIsProducedAroundLocalNoon()
    {
        var weights = ProductionShape.SlotWeights(96, daylightHours: 12);

        Array.IndexOf(weights, weights.Max()).Should().BeInRange(47, 48);
        weights[48].Should().BeGreaterThan(weights[36]).And.BeGreaterThan(weights[60]);
    }

    [Fact]
    public void ADayThatGainedOrLostAnHour_IsStillAWholeDaysEnergy()
    {
        foreach (var slots in new[] { 92, 96, 100 })
            ProductionShape.SlotWeights(slots, daylightHours: 10).Sum()
                .Should().BeApproximately(1.0, 1e-9, $"a day of {slots} slots");
    }

    [Fact]
    public void SummerDaysAreLongerThanWinterDays()
    {
        var midsummer = ProductionShape.DaylightHours(new DateOnly(2026, 6, 21));
        var midwinter = ProductionShape.DaylightHours(new DateOnly(2026, 12, 21));

        midsummer.Should().BeApproximately(15, 0.1);
        midwinter.Should().BeApproximately(9, 0.1);
        ProductionShape.DaylightHours(new DateOnly(2026, 3, 21)).Should().BeInRange(11, 13);
    }

    [Fact]
    public void NineSystemsInTen_AreHealthy()
    {
        var conditions = Enumerable.Range(0, 1000).Select(n => ProductionShape.ConditionOf(n / 1000.0)).ToList();

        conditions.Count(c => c == SystemCondition.Healthy).Should().Be(900);
        conditions.Count(c => c == SystemCondition.Degraded).Should().Be(70);
        conditions.Count(c => c == SystemCondition.Failing).Should().Be(30);
    }

    [Fact]
    public void AHealthySystem_ReachesNearlyAllOfWhatIsExpected_AndAFailingOneLittle()
    {
        ProductionShape.Reaches(SystemCondition.Healthy).Should().Be((0.95m, 1.00m));
        ProductionShape.Reaches(SystemCondition.Degraded).Should().Be((0.60m, 0.80m));
        ProductionShape.Reaches(SystemCondition.Failing).Low.Should().BeLessThan(0.60m);
    }
}
