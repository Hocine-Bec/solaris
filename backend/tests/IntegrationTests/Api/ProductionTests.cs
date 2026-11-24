using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// The three production views (FR-P2, FR-P3, FR-P4) over real readings. Built naive on purpose: every request adds
// the readings up again (P4b's before).
public class ProductionTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The day the fleet endpoint will pick: the last one finished in every zone these installations are in.</summary>
    private static DateOnly FleetDay(params string[] zones) =>
        LocalDays.LastDayEndedEverywhere(zones.Length == 0 ? [UsTimeZones.Central] : zones, DateTime.UtcNow);

    /// <summary>
    /// Backdates a switch-on, so the system was already producing on the day a view asks about. Through the API a
    /// system is switched on now, and the fleet's day has already ended.
    /// </summary>
    private async Task ActiveSinceAsync(int installationId, int daysAgo)
    {
        var at = DateTime.UtcNow.AddDays(-daysAgo);
        await Context.Database.ExecuteSqlAsync(
            $"UPDATE installations SET activated_at = {at}, status_changed_at = {at} WHERE id = {installationId}", Ct);
        Context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Readings written straight into the table: the endpoint sums whatever is there, and a device's own upload is
    /// limited to 30 days and 500 at a time (BR-T1), which these tests aren't about.
    /// </summary>
    private async Task GiveReadingsAsync(int installationId, DateOnly day, string zone, decimal totalKwh, int slots = 40)
    {
        var (fromUtc, _) = LocalDays.WindowUtc(day, zone);
        var each = totalKwh / slots;
        for (var n = 0; n < slots; n++)
            Context.EnergyReadings.Add(new EnergyReading
            {
                InstallationId = installationId,
                // From 06:00 local, so every reading is inside the day whatever its length
                RecordedAt = fromUtc.AddHours(6).AddMinutes(15 * n),
                EnergyKwh = each
            });
        await Context.SaveChangesAsync(Ct);
        Context.ChangeTracker.Clear();
    }

    private async Task<JsonElement> FleetAsync(HttpClient admin) =>
        await OkJsonAsync(admin.GetAsync("/api/production/fleet", Ct));

    private static int CountOf(JsonElement fleet, string health) =>
        fleet.GetProperty("counts").EnumerateArray().Single(c => c.GetProperty("health").GetString() == health)
            .GetProperty("count").GetInt32();

    [Fact]
    public async Task FR_P2_TheFleetsDay_CountsEveryHealth_AndNamesTheWorst()
    {
        // Arrange - one system that kept up and one that made almost nothing, on the same day
        var (admin, good, _) = await SwitchedOnAsync();
        var (_, poor, _) = await SwitchedOnAsync("2 Other St");
        var day = FleetDay();
        foreach (var id in new[] { good, poor })
            await ActiveSinceAsync(id, 10);
        var expected = Production.ExpectedKwh(8.4m, "TX", day.Month);
        await GiveReadingsAsync(good, day, UsTimeZones.Central, expected);
        await GiveReadingsAsync(poor, day, UsTimeZones.Central, expected * 0.1m);

        // Act
        var fleet = await FleetAsync(admin);

        // Assert - two systems, both reported, judged against their own expected production (a fleet of two is too
        // few for a median, ADR 15)
        fleet.GetProperty("day").GetString().Should().Be(day.ToString("yyyy-MM-dd"));
        (fleet.GetProperty("installations").GetInt32(), fleet.GetProperty("reporting").GetInt32()).Should().Be((2, 2));
        fleet.GetProperty("fleetMedianRatio").ValueKind.Should().Be(JsonValueKind.Null);
        (CountOf(fleet, "Excellent"), CountOf(fleet, "Critical"), CountOf(fleet, "Offline")).Should().Be((1, 1, 0));
        var worst = fleet.GetProperty("worst").EnumerateArray().Single();
        worst.GetProperty("installationId").GetInt32().Should().Be(poor);
        worst.GetProperty("health").GetString().Should().Be("Critical");
        worst.GetProperty("customerName").GetString().Should().Be("Jane Doe");
    }

    [Fact]
    public async Task BR_P2_ASystemThatSaidNothing_IsOffline_AndComesFirstAmongTheWorst()
    {
        // Arrange
        var (admin, reporting, _) = await SwitchedOnAsync();
        var (_, silent, _) = await SwitchedOnAsync("2 Other St");
        var day = FleetDay();
        foreach (var id in new[] { reporting, silent })
            await ActiveSinceAsync(id, 10);
        await GiveReadingsAsync(reporting, day, UsTimeZones.Central, Production.ExpectedKwh(8.4m, "TX", day.Month) * 0.5m);

        // Act
        var fleet = await FleetAsync(admin);

        // Assert
        (fleet.GetProperty("installations").GetInt32(), fleet.GetProperty("reporting").GetInt32()).Should().Be((2, 1));
        CountOf(fleet, "Offline").Should().Be(1);
        var worst = fleet.GetProperty("worst").EnumerateArray().ToList();
        worst.Should().HaveCount(2);
        worst[0].GetProperty("installationId").GetInt32().Should().Be(silent);
        worst[0].GetProperty("health").GetString().Should().Be("Offline");
        worst[0].GetProperty("actualKwh").GetDecimal().Should().Be(0m);
        worst[0].GetProperty("ratio").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ADR15_WithTenSystemsReporting_HealthIsMeasuredAgainstTheFleet()
    {
        // Arrange - eleven systems on a dark day: ten made a fifth of the table, one made a twentieth. Eleven quote
        // forms is past the limit per address (BR-S1), so the API runs with it off, as the measurement runs do.
        CreateClient(environment: "Development",
            settings: new Dictionary<string, string> { ["RateLimiting:Enabled"] = "false" }).Dispose();
        var (admin, first, _) = await SwitchedOnAsync();
        var ids = new List<int> { first };
        for (var n = 2; n <= 11; n++)
            ids.Add((await SwitchedOnAsync($"{n} Dark Day Ln")).Id);
        var day = FleetDay();
        foreach (var id in ids)
            await ActiveSinceAsync(id, 10);
        var expected = Production.ExpectedKwh(8.4m, "TX", day.Month);
        foreach (var id in ids.Take(10))
            await GiveReadingsAsync(id, day, UsTimeZones.Central, expected * 0.20m);
        await GiveReadingsAsync(ids[10], day, UsTimeZones.Central, expected * 0.05m);

        // Act
        var fleet = await FleetAsync(admin);

        // Assert - the ten that kept pace are Excellent though they made a fifth of the table; the one that didn't
        // is Critical
        fleet.GetProperty("reporting").GetInt32().Should().Be(11);
        fleet.GetProperty("fleetMedianRatio").GetDecimal().Should().BeApproximately(0.20m, 0.001m);
        (CountOf(fleet, "Excellent"), CountOf(fleet, "Critical")).Should().Be((10, 1));
        fleet.GetProperty("worst").EnumerateArray().Single().GetProperty("installationId").GetInt32().Should().Be(ids[10]);
    }

    [Fact]
    public async Task FR_P2_ADayStillRunningOutWest_IsNotCounted()
    {
        // Arrange - two systems, one in Hawaii, where the day ends five hours after Texas's
        var (admin, texas, _) = await SwitchedOnAsync();
        var (_, hawaii, _) = await SwitchedOnAsync("2 Island Rd");
        await Context.Database.ExecuteSqlAsync($"UPDATE installations SET time_zone = 'Pacific/Honolulu' WHERE id = {hawaii}", Ct);

        // Act
        var fleet = await FleetAsync(admin);

        // Assert - the day shown is the last one finished in Hawaii too, never the one still running there
        var day = DateOnly.Parse(fleet.GetProperty("day").GetString()!);
        day.Should().Be(LocalDays.LastDayEndedEverywhere([UsTimeZones.Central, UsTimeZones.Hawaii], DateTime.UtcNow));
        day.Should().BeOnOrBefore(LocalDays.LastDayEnded(UsTimeZones.Central, DateTime.UtcNow));
        texas.Should().NotBe(hawaii);
    }

    [Fact]
    public async Task FR_P2_WithNoActiveInstallations_TheDashboardIsEmpty_NotAFailure()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);

        var fleet = await FleetAsync(admin);

        fleet.GetProperty("installations").GetInt32().Should().Be(0);
        fleet.GetProperty("worst").GetArrayLength().Should().Be(0);
        fleet.GetProperty("counts").GetArrayLength().Should().Be(5);
        CountOf(fleet, "Offline").Should().Be(0);
    }

    [Fact]
    public async Task FR_P3_AMonth_ListsItsDays_FromTheDayItWasSwitchedOn()
    {
        // Arrange
        var (admin, id, _) = await SwitchedOnAsync();
        var day = LocalDays.LastDayEnded(UsTimeZones.Central, DateTime.UtcNow);
        await ActiveSinceAsync(id, 60);
        await GiveReadingsAsync(id, day, UsTimeZones.Central, 30m);

        // Act
        var month = await OkJsonAsync(admin.GetAsync($"/api/production/installations/{id}/years/{day.Year}/months/{day.Month}", Ct));

        // Assert - switched on 60 days ago, so every day of the month is listed, in order
        var days = month.GetProperty("days").EnumerateArray().ToList();
        days.Should().HaveCount(DateTime.DaysInMonth(day.Year, day.Month));
        days.Select(d => DateOnly.Parse(d.GetProperty("day").GetString()!)).Should().BeInAscendingOrder();
        var its = days.Single(d => d.GetProperty("day").GetString() == day.ToString("yyyy-MM-dd"));
        its.GetProperty("actualKwh").GetDecimal().Should().Be(30m);
        its.GetProperty("expectedKwh").GetDecimal().Should().Be(Production.ExpectedKwh(8.4m, "TX", day.Month));
        month.GetProperty("actualKwh").GetDecimal().Should().Be(30m);
        month.GetProperty("expectedKwh").GetDecimal().Should().Be(days.Sum(d => d.GetProperty("expectedKwh").GetDecimal()));
    }

    [Fact]
    public async Task FR_P4_AYear_TotalsEveryMonth_EvenTheEmptyOnes()
    {
        // Arrange
        var (admin, id, _) = await SwitchedOnAsync();
        var day = LocalDays.LastDayEnded(UsTimeZones.Central, DateTime.UtcNow);
        await ActiveSinceAsync(id, 60);
        await GiveReadingsAsync(id, day, UsTimeZones.Central, 42m);

        // Act
        var year = await OkJsonAsync(admin.GetAsync($"/api/production/installations/{id}/years/{day.Year}", Ct));

        // Assert
        var months = year.GetProperty("months").EnumerateArray().ToList();
        months.Should().HaveCount(12);
        months.Select(m => m.GetProperty("month").GetInt32()).Should().Equal(Enumerable.Range(1, 12));
        months.Single(m => m.GetProperty("month").GetInt32() == day.Month).GetProperty("actualKwh").GetDecimal().Should().Be(42m);
        year.GetProperty("actualKwh").GetDecimal().Should().Be(42m);
        // It only produced from the day it was switched on, so the months before that expect nothing of it
        var switchedOn = LocalDays.LocalDateOf(DateTime.UtcNow.AddDays(-60), UsTimeZones.Central);
        months.Where(m => m.GetProperty("month").GetInt32() < switchedOn.Month)
            .Should().OnlyContain(m => m.GetProperty("expectedKwh").GetDecimal() == 0m);
    }

    [Fact]
    public async Task AnInstallationThatIsntThere_Is404_AndANonsenseMonthOrYearIs400()
    {
        var (admin, id, _) = await SwitchedOnAsync();

        var unknown = await admin.GetAsync("/api/production/installations/9999/years/2026", Ct);
        var month13 = await admin.GetAsync($"/api/production/installations/{id}/years/2026/months/13", Ct);
        var month0 = await admin.GetAsync($"/api/production/installations/{id}/years/2026/months/0", Ct);
        var year1 = await admin.GetAsync($"/api/production/installations/{id}/years/1", Ct);

        await ShouldBeProblemAsync(unknown, HttpStatusCode.NotFound, "installation.not_found");
        foreach (var bad in new[] { month13, month0, year1 })
            await ShouldBeProblemAsync(bad, HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task AnInstallationNotSwitchedOn_HasNoDays()
    {
        // Arrange - converted, never switched on
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await ConvertedAsync(admin);

        // Act
        var year = await OkJsonAsync(admin.GetAsync($"/api/production/installations/{id}/years/{DateTime.UtcNow.Year}", Ct));
        var month = await OkJsonAsync(admin.GetAsync($"/api/production/installations/{id}/years/{DateTime.UtcNow.Year}/months/{DateTime.UtcNow.Month}", Ct));

        // Assert
        year.GetProperty("actualKwh").GetDecimal().Should().Be(0m);
        year.GetProperty("expectedKwh").GetDecimal().Should().Be(0m);
        month.GetProperty("days").GetArrayLength().Should().Be(0);
    }
}
