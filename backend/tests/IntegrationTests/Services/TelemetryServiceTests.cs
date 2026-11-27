using Application.DTOs.Telemetry;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;
using TestUtilities.Fakes;

namespace IntegrationTests.Services;

// TelemetryService with the real repositories and PostgreSQL. The API reaches it only with a key, which already
// names an Active installation (BR-T2); these tests ask the service itself, which doesn't take that on trust.
public class TelemetryServiceTests(PostgresFixture database) : IntegrationTestBase(database)
{
    private static readonly ManualClock Clock = new(new DateTimeOffset(2026, 11, 23, 14, 7, 0, TimeSpan.Zero));
    private static readonly DateTime Slot = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private int _seeded;

    private TelemetryService CreateService() =>
        new(new EnergyReadingRepo(Context), new InstallationRepo(Context), Clock);

    private static UploadReadingsRequest Batch(params (DateTime Slot, decimal Wh)[] readings) =>
        new() { Readings = readings.Select(r => new ReadingRequest { SlotStart = r.Slot, EnergyWh = r.Wh }).ToList() };

    /// <summary>An installation at a status, with its design recorded, as one that has been switched on has.</summary>
    private async Task<int> AnInstallationAsync(InstallationStatus status)
    {
        // Id 0: PostgreSQL gives each one its own, so several installations can be seeded in one test
        var address = AddressFactory.CreateFakeAddress(id: 0, street: $"{_seeded + 1} Pecan St");
        await SeedAsync(address);
        var customer = new Customer
        {
            FirstName = "Grace", LastName = "Kim", Email = $"grace.kim{_seeded}@example.com",
            NormalizedEmail = $"GRACE.KIM{_seeded}@EXAMPLE.COM", PhoneNumber = "(512) 555-0110",
            ContactAddressId = address.Id, Status = CustomerStatus.Active, RegistrationDate = Clock.GetUtcNow().UtcDateTime
        };
        await SeedAsync(customer);
        var lead = LeadFactory.CreateFakeLead(id: 0, addressId: address.Id, status: LeadStatus.Converted);
        await SeedAsync(lead);
        var installation = new Installation
        {
            CustomerId = customer.Id, AddressId = address.Id, LeadId = lead.Id, Status = status,
            TimeZone = "America/Chicago", SystemSizeKw = 8.4m, PanelCount = 21, InverterType = "Enphase IQ8M",
            CreatedAt = Clock.GetUtcNow().UtcDateTime, StatusChangedAt = Clock.GetUtcNow().UtcDateTime
        };
        await SeedAsync(installation);
        _seeded++;
        return installation.Id;
    }

    [Fact]
    public async Task BR_T2_OnlyAnActiveInstallationAcceptsReadings()
    {
        // Arrange - the same system before it is switched on, while Active, and after it is deactivated
        var active = await AnInstallationAsync(InstallationStatus.Active);
        var inspection = await AnInstallationAsync(InstallationStatus.Inspection);
        var deactivated = await AnInstallationAsync(InstallationStatus.Deactivated);

        // Act
        var accepted = await CreateService().UploadAsync(active, Batch((Slot, 1830)));
        var tooEarly = await CreateService().UploadAsync(inspection, Batch((Slot, 1830)));
        var tooLate = await CreateService().UploadAsync(deactivated, Batch((Slot, 1830)));
        var unknown = await CreateService().UploadAsync(999, Batch((Slot, 1830)));

        // Assert - only the Active one stored anything
        accepted.IsSuccess.Should().BeTrue();
        accepted.Value.Accepted.Should().Be(1);
        foreach (var refused in new[] { tooEarly, tooLate, unknown })
            refused.Error!.Code.Should().Be("device.invalid_key");
        ClearTracking();
        (await Context.EnergyReadings.SingleAsync(Ct)).InstallationId.Should().Be(active);
    }

    [Fact]
    public async Task FR_T2_ASlotTheInstallationAlreadyHas_IsADuplicate_NotAnError()
    {
        // Arrange
        var id = await AnInstallationAsync(InstallationStatus.Active);
        await CreateService().UploadAsync(id, Batch((Slot, 1830)));

        // Act - the same slot with a different value: the first one wins (BR-T1)
        var again = await CreateService().UploadAsync(id, Batch((Slot, 999), (Slot.AddMinutes(15), 1500)));

        // Assert
        again.Value.Should().BeEquivalentTo(new { Accepted = 1, Duplicates = 1 });
        again.Value.Rejected.Should().BeEmpty();
        ClearTracking();
        var stored = await Context.EnergyReadings.OrderBy(r => r.SlotStart).ToListAsync(Ct);
        stored.Select(r => r.EnergyWh).Should().Equal(1830, 1500);
    }

    [Fact]
    public async Task ASlotSentTwiceInOneBatch_IsStoredOnce()
    {
        // Arrange - a device that repeats itself inside one batch
        var id = await AnInstallationAsync(InstallationStatus.Active);

        // Act
        var answer = await CreateService().UploadAsync(id, Batch((Slot, 1830), (Slot, 1830)));

        // Assert
        answer.Value.Should().BeEquivalentTo(new { Accepted = 1, Duplicates = 1 });
        ClearTracking();
        (await Context.EnergyReadings.CountAsync(Ct)).Should().Be(1);
    }
}
