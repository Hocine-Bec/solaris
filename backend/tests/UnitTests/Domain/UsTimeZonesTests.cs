using Domain.Common;
using FluentAssertions;

namespace UnitTests.Domain;

public class UsTimeZonesTests
{
    [Fact]
    public void BR_P4_EveryStateAndDc_HasAProposedZone_FromTheList()
    {
        var ids = UsTimeZones.All.Select(z => z.Id).ToList();

        UsStates.Codes.Should().OnlyContain(state => ids.Contains(UsTimeZones.ProposedFor(state)));
    }

    [Fact]
    public void BR_P4_EveryZone_IsKnownToThisMachinesTimeZoneDatabase()
    {
        // The installation's local day is computed with these names; an unknown one would fail there
        UsTimeZones.All.Should().OnlyContain(z => TimeZoneInfo.FindSystemTimeZoneById(z.Id).Id == z.Id);
    }

    [Theory]
    [InlineData("TX", "America/Chicago")]
    [InlineData("NY", "America/New_York")]
    [InlineData("AZ", "America/Phoenix")]
    [InlineData("CO", "America/Denver")]
    [InlineData("CA", "America/Los_Angeles")]
    [InlineData("AK", "America/Anchorage")]
    [InlineData("HI", "Pacific/Honolulu")]
    [InlineData("IN", "America/New_York")]
    [InlineData("TN", "America/Chicago")]
    public void BR_P4_ProposedZone_IsWhereMostOfTheStateLives(string state, string zone)
    {
        UsTimeZones.ProposedFor(state).Should().Be(zone);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("tx")]
    public void ProposedFor_NotAStateCode_Throws_AndHasNoProposal(string state)
    {
        var act = () => UsTimeZones.ProposedFor(state);

        act.Should().Throw<ArgumentException>();
        UsTimeZones.ProposalFor(state).Should().BeNull();
    }

    [Fact]
    public void ProposalFor_GivesTheZonesName()
    {
        UsTimeZones.ProposalFor("AZ").Should().Be(("America/Phoenix", "Arizona"));
    }

    [Theory]
    [InlineData(UsTimeZones.Arizona, -7, -7)]
    [InlineData(UsTimeZones.Mountain, -7, -6)]
    [InlineData(UsTimeZones.Hawaii, -10, -10)]
    [InlineData(UsTimeZones.Aleutian, -10, -9)]
    public void TodaysRules_WinterAndSummerOffsets(string zone, int january, int july)
    {
        // Today's rules, not the zone's history: Phoenix had daylight saving in some past years, so .NET says the zone
        // "supports" it, but not since 1968
        var info = TimeZoneInfo.FindSystemTimeZoneById(zone);

        info.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)).Should().Be(TimeSpan.FromHours(january));
        info.GetUtcOffset(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)).Should().Be(TimeSpan.FromHours(july));
    }
}
