using Domain.Common;
using FluentAssertions;

namespace UnitTests.Domain;

// A day as the property lives it (BR-P4): its hours in UTC, and which day has finished there.
public class LocalDaysTests
{
    [Fact]
    public void AnOrdinaryDay_IsTwentyFourHours_StartingAtTheZonesMidnight()
    {
        var (from, to) = LocalDays.WindowUtc(new DateOnly(2026, 11, 23), UsTimeZones.Central);

        from.Should().Be(new DateTime(2026, 11, 23, 6, 0, 0, DateTimeKind.Utc));   // CST is UTC-6
        (to - from).Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public void BR_P4_TheDayTheClocksGoForward_IsTwentyThreeHours()
    {
        // 8 March 2026, 2 a.m. Central: 92 quarter hours
        var (from, to) = LocalDays.WindowUtc(new DateOnly(2026, 3, 8), UsTimeZones.Central);

        (to - from).Should().Be(TimeSpan.FromHours(23));
        ((to - from) / 15).Should().Be(TimeSpan.FromMinutes(92));
    }

    [Fact]
    public void BR_P4_TheDayTheClocksGoBack_IsTwentyFiveHours()
    {
        // 1 November 2026, 2 a.m. Central: 100 quarter hours
        var (from, to) = LocalDays.WindowUtc(new DateOnly(2026, 11, 1), UsTimeZones.Central);

        (to - from).Should().Be(TimeSpan.FromHours(25));
    }

    [Fact]
    public void BR_P4_AZoneWithoutDaylightSaving_AlwaysHasNinetySixSlots()
    {
        foreach (var day in new[] { new DateOnly(2026, 3, 8), new DateOnly(2026, 11, 1), new DateOnly(2026, 6, 15) })
            (LocalDays.WindowUtc(day, UsTimeZones.Arizona) is var (from, to) ? to - from : default)
                .Should().Be(TimeSpan.FromHours(24), $"{day} in Arizona");
    }

    [Fact]
    public void TwoZones_PutTheSameMomentOnDifferentDays()
    {
        // 06:30 UTC on 23 November: still the 22nd in Hawaii, already the 23rd in New York
        var moment = new DateTime(2026, 11, 23, 6, 30, 0, DateTimeKind.Utc);

        LocalDays.LocalDateOf(moment, UsTimeZones.Eastern).Should().Be(new DateOnly(2026, 11, 23));
        LocalDays.LocalDateOf(moment, UsTimeZones.Hawaii).Should().Be(new DateOnly(2026, 11, 22));
    }

    [Fact]
    public void TheLastDayEnded_IsTheOneBeforeTheDayUnderWay()
    {
        var noonEastern = new DateTime(2026, 11, 23, 17, 0, 0, DateTimeKind.Utc);

        LocalDays.LastDayEnded(UsTimeZones.Eastern, noonEastern).Should().Be(new DateOnly(2026, 11, 22));
    }

    [Fact]
    public void FR_P2_TheFleetsLastDay_IsTheEarliestOfItsZones()
    {
        // 06:30 UTC: Eastern is into the 23rd, Hawaii still in the 22nd, so the 21st is the last day ended in both
        var moment = new DateTime(2026, 11, 23, 6, 30, 0, DateTimeKind.Utc);

        LocalDays.LastDayEndedEverywhere([UsTimeZones.Eastern], moment).Should().Be(new DateOnly(2026, 11, 22));
        LocalDays.LastDayEndedEverywhere([UsTimeZones.Eastern, UsTimeZones.Hawaii], moment)
            .Should().Be(new DateOnly(2026, 11, 21));
    }

    [Fact]
    public void EveryZoneAnInstallationCanBeIn_HasItsDays()
    {
        var day = new DateOnly(2026, 6, 15);

        foreach (var (id, _) in UsTimeZones.All)
        {
            var (from, to) = LocalDays.WindowUtc(day, id);
            (to - from).Should().Be(TimeSpan.FromHours(24), id);
            LocalDays.LocalDateOf(from, id).Should().Be(day, id);
        }
    }
}
