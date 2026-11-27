using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Production;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

// What the systems produced (FR-P2, FR-P3, FR-P4). The rules are the Domain's: Production for what a day should make
// and how it went (BR-P1, BR-P2, ADR 15), LocalDays for which day is which (BR-P4).
//
// The views read the daily rollups (EnergyProduction), one row per installation and local day, which RollupJob adds
// up from the readings. A rollup holds only what the readings say: what a day should have made comes from the
// system's design, and how it went from the fleet's day, both worked out here. A day without a row is a day the
// system said nothing: Offline on the fleet's view, nothing produced on the month's and the year's. The first version
// added the raw readings up again on every request; P4b measures the difference.
public class ProductionService(
    IEnergyProductionRepo production,
    IInstallationRepo installations,
    TimeProvider time) : IProductionService
{
    /// <summary>How many of the worst performers the dashboard names (FR-P2).</summary>
    private const int WorstShown = 20;

    public async Task<FleetDayResponse> GetFleetDayAsync()
    {
        var active = await installations.GetActiveFleetAsync();
        if (active.Count == 0)
            return new FleetDayResponse(DateOnly.FromDateTime(Now()), 0, 0, null, EveryHealthCounted([]), []);

        // The day is the last one that has finished in every zone the fleet stands in, so none is still running
        var day = LocalDays.LastDayEndedEverywhere(active.Select(i => i.TimeZone).Distinct(), Now());

        // A system switched on since that day wasn't there to produce anything: it isn't counted as Offline for a day
        // it didn't live through.
        var fleet = active.Where(i => Produced(i, day, i.TimeZone)).ToList();
        if (fleet.Count == 0)
            return new FleetDayResponse(day, 0, 0, null, EveryHealthCounted([]), []);

        // The day's rows, one read: each installation's row is already its own local day (BR-P4). A row added up
        // from no readings at all means the same as no row.
        var actualByInstallation = (await production.GetDayAsync(day))
            .Where(r => r.ReadingCount > 0)
            .ToDictionary(r => r.InstallationId, r => Kwh(r.ActualWh));
        actualByInstallation = fleet.Where(i => actualByInstallation.ContainsKey(i.Id))
            .ToDictionary(i => i.Id, i => actualByInstallation[i.Id]);

        var days = fleet.Select(i =>
        {
            var expected = ExpectedFor(i, day.Month);
            var actual = actualByInstallation.TryGetValue(i.Id, out var kwh) ? kwh : (decimal?)null;
            return (Installation: i, Expected: expected, Actual: actual, Ratio: RatioOf(actual, expected));
        }).ToList();

        // Health is measured against the middle of the day's ratios, so the weather moves everyone together (ADR 15)
        var median = Production.FleetMedianRatio(days.Where(d => d.Ratio is not null).Select(d => d.Ratio!.Value).ToList());
        var judged = days
            .Select(d => (d.Installation, d.Expected, d.Actual, d.Ratio,
                Health: Production.HealthOf(d.Actual, d.Expected, median)))
            .ToList();

        var worst = judged
            .Where(d => d.Health != ProductionHealth.Excellent)
            .OrderBy(d => d.Ratio ?? -1m).ThenBy(d => d.Installation.Id)
            .Take(WorstShown)
            .Select(d => new WorstPerformerResponse(d.Installation.Id, AddressResponse.From(d.Installation.Address),
                d.Installation.Customer.FullName, d.Actual ?? 0m, d.Expected, d.Ratio, d.Health.ToString()))
            .ToList();

        return new FleetDayResponse(day, fleet.Count, actualByInstallation.Count, median,
            EveryHealthCounted(judged.Select(d => d.Health)), worst);
    }

    public async Task<Result<MonthProductionResponse>> GetMonthAsync(int installationId, int year, int month)
    {
        if (await installations.GetWithAddressAsync(installationId) is not { } installation)
            return Result.Failure<MonthProductionResponse>(InstallationErrors.NotFound(installationId));

        var first = new DateOnly(year, month, 1);
        var days = await DailyAsync(installation, first, first.AddMonths(1));
        return Result.Success(new MonthProductionResponse(installationId, year, month,
            days.Sum(d => d.ActualKwh), days.Sum(d => d.ExpectedKwh), days));
    }

    public async Task<Result<YearProductionResponse>> GetYearAsync(int installationId, int year)
    {
        if (await installations.GetWithAddressAsync(installationId) is not { } installation)
            return Result.Failure<YearProductionResponse>(InstallationErrors.NotFound(installationId));

        var first = new DateOnly(year, 1, 1);
        var days = await DailyAsync(installation, first, first.AddYears(1));
        var months = Enumerable.Range(1, 12).Select(month =>
        {
            var its = days.Where(d => d.Day.Month == month).ToList();
            return new MonthProductionTotalResponse(month, its.Sum(d => d.ActualKwh), its.Sum(d => d.ExpectedKwh));
        }).ToList();

        return Result.Success(new YearProductionResponse(installationId, year,
            months.Sum(m => m.ActualKwh), months.Sum(m => m.ExpectedKwh), months));
    }

    // ----

    /// <summary>
    /// One installation's local days between two dates, each with what it made and what it should have. A day before
    /// the system was switched on, or after it stopped, isn't a day of production and isn't listed.
    /// </summary>
    private async Task<IReadOnlyList<DayProductionResponse>> DailyAsync(Installation installation, DateOnly from, DateOnly to)
    {
        var zone = installation.TimeZone;
        // The rollups are already its local days (BR-P4): one row a day, read by the key
        var actualByDay = (await production.GetDaysAsync(installation.Id, from, to))
            .ToDictionary(r => r.Day, r => Kwh(r.ActualWh));

        var produced = new List<DayProductionResponse>();
        for (var day = from; day < to; day = day.AddDays(1))
        {
            if (!Produced(installation, day, zone))
                continue;
            produced.Add(new DayProductionResponse(day,
                actualByDay.TryGetValue(day, out var kwh) ? kwh : 0m, ExpectedFor(installation, day.Month)));
        }
        return produced;
    }

    /// <summary>Whether the system was switched on, and not yet stopped, on this local day.</summary>
    private static bool Produced(Installation installation, DateOnly day, string zone)
    {
        if (installation.ActivatedAt is not { } activatedAt || day < LocalDays.LocalDateOf(activatedAt, zone))
            return false;
        return installation.Status != InstallationStatus.Deactivated
               || day <= LocalDays.LocalDateOf(installation.StatusChangedAt, zone);
    }

    private static decimal ExpectedFor(Installation installation, int month) =>
        installation.SystemSizeKw is { } size ? Production.ExpectedKwh(size, installation.Address.State, month) : 0m;

    /// <summary>Watt-hours as the kilowatt-hours the views report.</summary>
    private static decimal Kwh(long wh) => wh / (decimal)EnergyReading.WhPerKwh;

    private static decimal? RatioOf(decimal? actual, decimal expected) =>
        actual is { } kwh && expected > 0 ? kwh / expected : null;

    /// <summary>Every health with its count, worst last, so a screen can draw them all without inventing zeroes.</summary>
    private static IReadOnlyList<HealthCountResponse> EveryHealthCounted(IEnumerable<ProductionHealth> healths)
    {
        var counted = healths.GroupBy(h => h).ToDictionary(g => g.Key, g => g.Count());
        return Enum.GetValues<ProductionHealth>()
            .Select(health => new HealthCountResponse(health.ToString(), counted.GetValueOrDefault(health)))
            .ToList();
    }

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
