using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Infrastructure;

// RollupJob on PostgreSQL (BR-P3, BR-P4): the marked days added up from their readings, over each installation's own
// local hours, as often as asked and by as many runs at once as there are.
public class RollupJobTests(PostgresFixture database) : IntegrationTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private int _seeded;

    private RollupJob Job(int batchSize = 1000) => new(CreateContext(), NullLogger<RollupJob>.Instance) { BatchSize = batchSize };

    private async Task<int> AnInstallationAsync(string timeZone = UsTimeZones.Central)
    {
        var address = AddressFactory.CreateFakeAddress(id: 0, street: $"{_seeded + 1} Rollup Rd");
        await SeedAsync(address);
        var customer = new Customer
        {
            FirstName = "Grace", LastName = "Kim", Email = $"rollup{_seeded}@example.com",
            NormalizedEmail = $"ROLLUP{_seeded}@EXAMPLE.COM", PhoneNumber = "(512) 555-0110",
            ContactAddressId = address.Id, Status = CustomerStatus.Active, RegistrationDate = DateTime.UtcNow
        };
        await SeedAsync(customer);
        var lead = LeadFactory.CreateFakeLead(id: 0, addressId: address.Id, status: LeadStatus.Converted);
        await SeedAsync(lead);
        var installation = new Installation
        {
            CustomerId = customer.Id, AddressId = address.Id, LeadId = lead.Id, Status = InstallationStatus.Active,
            TimeZone = timeZone, SystemSizeKw = 8.4m, PanelCount = 21, InverterType = "Enphase IQ8M",
            CreatedAt = DateTime.UtcNow, StatusChangedAt = DateTime.UtcNow, ActivatedAt = DateTime.UtcNow.AddDays(-60)
        };
        await SeedAsync(installation);
        _seeded++;
        return installation.Id;
    }

    /// <summary>A reading at a moment, and its local day marked, as the ingestion's one statement does both.</summary>
    private async Task ReadAsync(int installationId, DateTime slotStart, int wh, string timeZone = UsTimeZones.Central)
    {
        await Context.Database.ExecuteSqlAsync($"""
            INSERT INTO energy_readings (installation_id, slot_start, energy_wh) VALUES ({installationId}, {slotStart}, {wh});
            INSERT INTO production_dirty_days (installation_id, day)
            VALUES ({installationId}, {LocalDays.LocalDateOf(slotStart, timeZone)}) ON CONFLICT DO NOTHING;
            """, Ct);
    }

    private async Task<EnergyProduction?> DayAsync(int installationId, DateOnly day) =>
        await CreateContext().EnergyProduction.AsNoTracking()
            .SingleOrDefaultAsync(p => p.InstallationId == installationId && p.Day == day, Ct);

    [Fact]
    public async Task AMarkedDay_IsAddedUpFromItsReadings_AndItsMarkCleared()
    {
        // Arrange - three readings on 20 November, Central
        var id = await AnInstallationAsync();
        var (from, _) = LocalDays.WindowUtc(new DateOnly(2026, 11, 20), UsTimeZones.Central);
        foreach (var (hour, wh) in new[] { (9, 1000), (12, 2500), (15, 800) })
            await ReadAsync(id, from.AddHours(hour), wh);

        // Act
        var result = await Job().RunAsync(Ct);

        // Assert
        result.Days.Should().Be(1);
        var day = await DayAsync(id, new DateOnly(2026, 11, 20));
        (day!.ActualWh, day.ReadingCount).Should().Be((4300, 3));
        (await Context.ProductionDirtyDays.CountAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task BR_P3_ADayAddedUpTwice_IsTheSameRow()
    {
        // Arrange
        var id = await AnInstallationAsync();
        var (from, _) = LocalDays.WindowUtc(new DateOnly(2026, 11, 20), UsTimeZones.Central);
        await ReadAsync(id, from.AddHours(12), 2500);
        await Job().RunAsync(Ct);
        var first = await DayAsync(id, new DateOnly(2026, 11, 20));

        // Act - the same day marked again with nothing new, and added up again
        await Context.Database.ExecuteSqlAsync(
            $"INSERT INTO production_dirty_days (installation_id, day) VALUES ({id}, {new DateOnly(2026, 11, 20)})", Ct);
        await Job().RunAsync(Ct);

        // Assert
        var again = await DayAsync(id, new DateOnly(2026, 11, 20));
        (again!.ActualWh, again.ReadingCount).Should().Be((first!.ActualWh, first.ReadingCount));
        (await Context.EnergyProduction.CountAsync(Ct)).Should().Be(1);
    }

    [Fact]
    public async Task BR_P3_ALateReading_ChangesOnlyItsOwnDay()
    {
        // Arrange - two days already added up
        var id = await AnInstallationAsync();
        var (the20th, _) = LocalDays.WindowUtc(new DateOnly(2026, 11, 20), UsTimeZones.Central);
        var (the21st, _) = LocalDays.WindowUtc(new DateOnly(2026, 11, 21), UsTimeZones.Central);
        await ReadAsync(id, the20th.AddHours(12), 2500);
        await ReadAsync(id, the21st.AddHours(12), 3000);
        await Job().RunAsync(Ct);

        // Act - a reading for the 20th arrives days later
        await ReadAsync(id, the20th.AddHours(13), 1000);
        var result = await Job().RunAsync(Ct);

        // Assert - only the 20th was added up again, and now holds it
        result.Days.Should().Be(1);
        (await DayAsync(id, new DateOnly(2026, 11, 20)))!.ActualWh.Should().Be(3500);
        (await DayAsync(id, new DateOnly(2026, 11, 21)))!.ActualWh.Should().Be(3000);
    }

    [Fact]
    public async Task BR_P4_AReadingBelongsToTheLocalDay_NotTheUtcOne()
    {
        // Arrange - 05:30 UTC on 21 November is still the 20th in Chicago (UTC-6)
        var id = await AnInstallationAsync();
        await ReadAsync(id, new DateTime(2026, 11, 21, 5, 30, 0, DateTimeKind.Utc), 700);
        await ReadAsync(id, new DateTime(2026, 11, 21, 6, 0, 0, DateTimeKind.Utc), 900);

        // Act
        await Job().RunAsync(Ct);

        // Assert
        (await DayAsync(id, new DateOnly(2026, 11, 20)))!.ActualWh.Should().Be(700);
        (await DayAsync(id, new DateOnly(2026, 11, 21)))!.ActualWh.Should().Be(900);
    }

    [Theory]
    [InlineData(2026, 3, 8, 92)]     // the clocks go forward: 23 hours
    [InlineData(2026, 11, 1, 100)]   // the clocks go back: 25 hours
    [InlineData(2026, 6, 15, 96)]
    public async Task BR_P4_ADaylightSavingDay_IsAllOfItsSlots(int year, int month, int dayOfMonth, int slots)
    {
        // Arrange - a reading in every slot of the local day, and one either side of it
        var id = await AnInstallationAsync();
        var day = new DateOnly(year, month, dayOfMonth);
        var (from, to) = LocalDays.WindowUtc(day, UsTimeZones.Central);
        for (var at = from; at < to; at = at.AddMinutes(15))
            await ReadAsync(id, at, 10);
        await ReadAsync(id, from.AddMinutes(-15), 10);
        await ReadAsync(id, to, 10);

        // Act
        await Job().RunAsync(Ct);

        // Assert
        (await DayAsync(id, day))!.ReadingCount.Should().Be(slots);
    }

    [Fact]
    public async Task BR_P4_ArizonasDay_IsAlwaysNinetySixSlots()
    {
        // Arrange - the day Chicago's clocks go back; Arizona's don't move
        var id = await AnInstallationAsync(UsTimeZones.Arizona);
        var day = new DateOnly(2026, 11, 1);
        var (from, to) = LocalDays.WindowUtc(day, UsTimeZones.Arizona);
        for (var at = from; at < to; at = at.AddMinutes(15))
            await ReadAsync(id, at, 10, UsTimeZones.Arizona);

        // Act
        await Job().RunAsync(Ct);

        // Assert
        (await DayAsync(id, day))!.ReadingCount.Should().Be(96);
    }

    [Fact]
    public async Task BR_P4_TwoZones_AddUpTheSameDateOverTheirOwnHours()
    {
        // Arrange - one moment, 07:00 UTC on 21 November: the 21st in New York, still the 20th in Hawaii
        var eastern = await AnInstallationAsync(UsTimeZones.Eastern);
        var hawaii = await AnInstallationAsync(UsTimeZones.Hawaii);
        var moment = new DateTime(2026, 11, 21, 7, 0, 0, DateTimeKind.Utc);
        await ReadAsync(eastern, moment, 500, UsTimeZones.Eastern);
        await ReadAsync(hawaii, moment, 600, UsTimeZones.Hawaii);

        // Act
        await Job().RunAsync(Ct);

        // Assert
        (await DayAsync(eastern, new DateOnly(2026, 11, 21)))!.ActualWh.Should().Be(500);
        (await DayAsync(hawaii, new DateOnly(2026, 11, 20)))!.ActualWh.Should().Be(600);
    }

    [Fact]
    public async Task MoreMarkedDaysThanABatch_AreAllAddedUp_InSeveralBatches()
    {
        // Arrange - 25 days, with batches of 10
        var id = await AnInstallationAsync();
        for (var n = 0; n < 25; n++)
        {
            var (from, _) = LocalDays.WindowUtc(new DateOnly(2026, 10, 1).AddDays(n), UsTimeZones.Central);
            await ReadAsync(id, from.AddHours(12), 100 + n);
        }

        // Act
        var result = await Job(batchSize: 10).RunAsync(Ct);

        // Assert
        (result.Batches, result.Days).Should().Be((3, 25));
        (await Context.EnergyProduction.CountAsync(Ct)).Should().Be(25);
        (await Context.ProductionDirtyDays.CountAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task TwoRunsAtOnce_NeverTakeTheSameDay()
    {
        // Arrange - 200 marked days, and two jobs released together with small batches, so they overlap
        var id = await AnInstallationAsync();
        for (var n = 0; n < 200; n++)
        {
            var (from, _) = LocalDays.WindowUtc(new DateOnly(2026, 5, 1).AddDays(n), UsTimeZones.Central);
            await ReadAsync(id, from.AddHours(12), 100);
        }
        using var start = new SemaphoreSlim(0);
        var runs = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            await start.WaitAsync(Ct);
            return await Job(batchSize: 10).RunAsync(Ct);
        }, Ct)).ToList();

        // Act
        start.Release(2);
        var results = await Task.WhenAll(runs);

        // Assert - between them every day once: SKIP LOCKED let each take what the other hadn't
        results.Sum(r => r.Days).Should().Be(200);
        (await Context.EnergyProduction.CountAsync(Ct)).Should().Be(200);
    }

    [Fact]
    public async Task NothingMarked_IsNothingToDo()
    {
        var result = await Job().RunAsync(Ct);

        (result.Batches, result.Days).Should().Be((0, 0));
    }
}
