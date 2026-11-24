using Domain.Entities;
using FluentAssertions;

namespace UnitTests.Domain;

// Which readings a system's own data may add (BR-T1), on the entity.
public class EnergyReadingTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 14, 7, 0, DateTimeKind.Utc);
    private const decimal SizeKw = 8.4m;

    /// <summary>The most an 8.4 kW system can make in a quarter of an hour, with the tenth BR-T1 allows: 2.31 kWh.</summary>
    private static decimal Maximum => EnergyReading.MaximumKwh(SizeKw);

    private static string? Why(DateTime slot, decimal kwh, decimal? size = SizeKw) =>
        EnergyReading.WhyNotAccepted(slot, kwh, size, Now)?.Code;

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    public void AQuarterHourSlot_IsAccepted(int minute)
    {
        Why(new DateTime(2026, 11, 23, 13, minute, 0, DateTimeKind.Utc), 1.5m).Should().BeNull();
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]    // a minute past the hour
    [InlineData(0, 14, 0, 0)]   // a minute short of the slot
    [InlineData(0, 15, 1, 0)]   // a second past the slot
    [InlineData(0, 15, 0, 1)]   // a millisecond past it
    public void ASlotOffTheQuarterHour_IsRefused(int hour, int minute, int second, int millisecond)
    {
        var slot = new DateTime(2026, 11, 23, hour, minute, second, millisecond, DateTimeKind.Utc);

        Why(slot, 1.5m).Should().Be("reading.not_a_slot");
    }

    [Fact]
    public void TheSlotHappeningNow_IsStillToCome()
    {
        // 14:07: the 14:15 slot hasn't started, and 14:00's is under way and may be sent
        Why(new DateTime(2026, 11, 23, 14, 15, 0, DateTimeKind.Utc), 1m).Should().Be("reading.in_the_future");
        Why(new DateTime(2026, 11, 23, 14, 0, 0, DateTimeKind.Utc), 1m).Should().BeNull();
    }

    [Fact]
    public void ThirtyDaysBack_IsTheLimit()
    {
        // 30 days before 23 Nov 14:07 is 24 Oct 14:07: the slot after it is still accepted, the one before it isn't
        var oldest = Now - EnergyReading.OldestAccepted;
        oldest.Should().Be(new DateTime(2026, 10, 24, 14, 7, 0, DateTimeKind.Utc));

        Why(new DateTime(2026, 10, 24, 14, 15, 0, DateTimeKind.Utc), 1m).Should().BeNull();
        Why(new DateTime(2026, 10, 24, 14, 0, 0, DateTimeKind.Utc), 1m).Should().Be("reading.too_old");
    }

    [Fact]
    public void NothingProduced_IsAReading_ButLessThanNothingIsNot()
    {
        var slot = new DateTime(2026, 11, 23, 3, 0, 0, DateTimeKind.Utc);

        Why(slot, 0m).Should().BeNull();
        Why(slot, -0.001m).Should().Be("reading.negative");
    }

    [Fact]
    public void MoreThanTheSystemCanMake_IsRefused()
    {
        var slot = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

        Maximum.Should().Be(2.31m);
        Why(slot, Maximum).Should().BeNull();
        Why(slot, Maximum + 0.001m).Should().Be("reading.above_the_maximum");
        EnergyReading.WhyNotAccepted(slot, Maximum + 0.001m, SizeKw, Now)!.Message
            .Should().Be("More than a 8.4 kW system can produce in 15 minutes (2.31 kWh).");
    }

    [Fact]
    public void WithoutADesign_TheMaximumCantBeChecked_AndTheOtherRulesStillAre()
    {
        var slot = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

        Why(slot, 9999m, size: null).Should().BeNull();
        Why(slot, -1m, size: null).Should().Be("reading.negative");
    }

    [Fact]
    public void TheFirstRuleThatRefuses_IsTheOneReported()
    {
        // A slot off the quarter hour, in the future and too big at once: the device is told about its clock first
        var bad = new DateTime(2027, 1, 1, 10, 7, 0, DateTimeKind.Utc);

        Why(bad, 99m).Should().Be("reading.not_a_slot");
    }
}
