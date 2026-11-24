using Domain.Common;
using FluentAssertions;

namespace UnitTests.Domain;

// The table BR-P1 computes expected production from, as it was read from the lab's published data
// (docs/data/peak-sun-hours.json).
public class PeakSunHoursTests
{
    [Fact]
    public void EveryStateSolarisWorksIn_HasTwelveMonths()
    {
        foreach (var state in UsStates.Codes)
        {
            PeakSunHours.Knows(state).Should().BeTrue($"{state} must have peak sun hours");
            for (var month = 1; month <= 12; month++)
                PeakSunHours.For(state, month).Should().BeGreaterThan(0, $"{state} in month {month}");
        }
    }

    [Fact]
    public void TheHoursAreInTheRangeTheUnitedStatesSees()
    {
        // Between nothing and the ~8.5 kWh/m² a desert gets in June: a number outside this is a wrong column or unit
        var all = UsStates.Codes.SelectMany(s => Enumerable.Range(1, 12).Select(m => PeakSunHours.For(s, m))).ToList();

        all.Should().OnlyContain(hours => hours > 0m && hours < 9m);
        all.Max().Should().Be(PeakSunHours.For("NV", 6));   // Las Vegas in June, the sunniest row
        all.Min().Should().Be(PeakSunHours.For("AK", 12));  // Anchorage in December, the darkest
    }

    [Theory]
    [InlineData("TX", 6, 6.13)]   // Houston
    [InlineData("AZ", 6, 8.31)]   // Phoenix
    [InlineData("WA", 12, 0.84)]  // Seattle
    [InlineData("AK", 12, 0.17)]  // Anchorage, from PVWatts: the other dataset stops at the lower 48 and Hawaii
    public void AStatesMonth_IsWhatTheSourceSays(string state, int month, decimal hours)
    {
        PeakSunHours.For(state, month).Should().Be(hours);
    }

    [Fact]
    public void Summer_BeatsWinter_EverywhereInTheUnitedStates()
    {
        foreach (var state in UsStates.Codes)
            PeakSunHours.For(state, 6).Should().BeGreaterThan(PeakSunHours.For(state, 12), $"June beats December in {state}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void AMonthOutsideTheYear_IsRefused(int month)
    {
        var act = () => PeakSunHours.For("TX", month);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("ZZ")]
    [InlineData("tx")]
    [InlineData("Texas")]
    public void AStateTheTableDoesntHave_IsABug(string state)
    {
        PeakSunHours.Knows(state).Should().BeFalse();
        var act = () => PeakSunHours.For(state, 6);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
