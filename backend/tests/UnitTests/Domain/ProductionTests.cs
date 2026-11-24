using Domain.Common;
using Domain.Enums;
using FluentAssertions;

namespace UnitTests.Domain;

// What a day should produce and how it is judged (BR-P1, BR-P2, ADR 15).
public class ProductionTests
{
    // Houston in June: 6.13 peak sun hours, so an 8.4 kW system should make 8.4 × 6.13 × 0.8 = 41.1936 kWh
    private const decimal SizeKw = 8.4m;
    private static decimal Expected => Production.ExpectedKwh(SizeKw, "TX", 6);

    [Fact]
    public void BR_P1_Expected_IsSizeTimesTheMonthsSunTimesThePerformanceRatio()
    {
        Expected.Should().Be(8.4m * 6.13m * 0.80m);
        Expected.Should().BeApproximately(41.19m, 0.01m);
    }

    [Fact]
    public void BR_P1_TheSameSystem_ExpectsLessInWinterThanInSummer()
    {
        Production.ExpectedKwh(SizeKw, "TX", 12).Should().BeLessThan(Production.ExpectedKwh(SizeKw, "TX", 6));
    }

    [Theory]
    [InlineData(1.00, ProductionHealth.Excellent)]
    [InlineData(0.90, ProductionHealth.Excellent)]
    [InlineData(0.899, ProductionHealth.Good)]
    [InlineData(0.80, ProductionHealth.Good)]
    [InlineData(0.799, ProductionHealth.Warning)]
    [InlineData(0.60, ProductionHealth.Warning)]
    [InlineData(0.599, ProductionHealth.Critical)]
    [InlineData(0.00, ProductionHealth.Critical)]
    public void BR_P2_WithoutAFleet_TheRatioStandsOnItsOwn(decimal ratio, ProductionHealth health)
    {
        Production.HealthOf(Expected * ratio, Expected, fleetMedianRatio: null).Should().Be(health);
    }

    [Fact]
    public void BR_P2_NoReadingsAtAll_IsOffline_WhateverTheFleetDid()
    {
        Production.HealthOf(null, Expected, null).Should().Be(ProductionHealth.Offline);
        Production.HealthOf(null, Expected, 0.95m).Should().Be(ProductionHealth.Offline);
        Production.HealthOf(null, Expected, 0.10m).Should().Be(ProductionHealth.Offline);
    }

    [Fact]
    public void BR_P2_NothingProducedButTheSystemReported_IsCritical_NotOffline()
    {
        Production.HealthOf(0m, Expected, null).Should().Be(ProductionHealth.Critical);
    }

    [Fact]
    public void ADR15_OnADarkDay_ASystemThatKeptPaceWithTheFleet_IsHealthy()
    {
        // Everyone made a fifth of what the table expects: the weather, not 1,000 broken systems
        var fleetMedian = 0.20m;
        var kept = Expected * 0.20m;
        var behind = Expected * 0.10m;

        Production.HealthOf(kept, Expected, fleetMedian).Should().Be(ProductionHealth.Excellent);
        Production.HealthOf(behind, Expected, fleetMedian).Should().Be(ProductionHealth.Critical);
        // ...and without the fleet, the same good system would have been called Critical
        Production.HealthOf(kept, Expected, null).Should().Be(ProductionHealth.Critical);
    }

    [Fact]
    public void ADR15_OnABrightDay_ASystemBehindTheFleet_IsNotSavedByTheTable()
    {
        // The fleet beat the table (a clear, cool day), so merely meeting the table is off the pace: 1 ÷ 1.2 = 0.83
        var metTheTable = Expected;

        Production.HealthOf(metTheTable, Expected, 1.20m).Should().Be(ProductionHealth.Good);
        Production.HealthOf(metTheTable, Expected, 1.40m).Should().Be(ProductionHealth.Warning);
        Production.HealthOf(metTheTable, Expected, 1.80m).Should().Be(ProductionHealth.Critical);
        // ...where the table alone would have called every one of them Excellent
        Production.HealthOf(metTheTable, Expected, null).Should().Be(ProductionHealth.Excellent);
    }

    [Fact]
    public void BR_P2_AFleetMedianOfZero_IsIgnored_SoNobodyIsDividedByNothing()
    {
        // Every system that reported made nothing: the ratio is read as it is
        Production.HealthOf(0m, Expected, 0m).Should().Be(ProductionHealth.Critical);
        Production.HealthOf(Expected, Expected, 0m).Should().Be(ProductionHealth.Excellent);
    }

    [Fact]
    public void BR_P2_ASystemThatShouldProduceNothing_CantFail()
    {
        Production.HealthOf(0m, 0m, null).Should().Be(ProductionHealth.Excellent);
    }

    [Fact]
    public void ADR15_TooFewSystemsReported_HasNoMedian()
    {
        var nine = Enumerable.Repeat(0.9m, Production.SmallestFleet - 1).ToList();
        var ten = Enumerable.Repeat(0.9m, Production.SmallestFleet).ToList();

        Production.SmallestFleet.Should().Be(10);
        Production.FleetMedianRatio(nine).Should().BeNull();
        Production.FleetMedianRatio(ten).Should().Be(0.9m);
        Production.FleetMedianRatio([]).Should().BeNull();
    }

    [Fact]
    public void ADR15_TheMedian_IsTheMiddleRatio_AndAFewBrokenSystemsDontMoveIt()
    {
        // Eleven systems: two dead, nine around 0.9. The middle is one of the nine.
        var ratios = new[] { 0m, 0.02m, 0.86m, 0.88m, 0.89m, 0.90m, 0.91m, 0.92m, 0.93m, 0.95m, 1.05m };

        Production.FleetMedianRatio(ratios).Should().Be(0.90m);
    }

    [Fact]
    public void ADR15_AnEvenFleet_TakesTheMiddleOfTheTwoMiddleRatios()
    {
        var ratios = new[] { 0.1m, 0.2m, 0.3m, 0.4m, 0.50m, 0.70m, 0.8m, 0.9m, 1.0m, 1.1m };

        Production.FleetMedianRatio(ratios).Should().Be(0.60m);
    }
}
