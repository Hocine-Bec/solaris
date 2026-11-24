using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// The readings endpoint as a device calls it (FR-T1, FR-T2, BR-T1, BR-T2): the only endpoint a device can reach.
// Built naive on purpose, so two batches at the same moment can store one slot twice (P4a's before).
public class TelemetryTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A slot earlier today, rounded down to the quarter hour, so it is always in the past and not too old.</summary>
    private static DateTime Slot(int hoursAgo = 2)
    {
        var at = DateTime.UtcNow.AddHours(-hoursAgo);
        return new DateTime(at.Year, at.Month, at.Day, at.Hour, at.Minute / 15 * 15, 0, DateTimeKind.Utc);
    }

    private static object Reading(DateTime slot, decimal wh) => new { slotStart = slot, energyWh = wh };

    /// <summary>An Active installation and a client that holds its device key, as a device does.</summary>
    private async Task<(HttpClient Device, int Id)> ADeviceAsync()
    {
        var (_, id, switchedOn) = await SwitchedOnAsync();
        var key = (await switchedOn.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("issuedKey").GetString();
        var device = CreateClient();
        device.DefaultRequestHeaders.Add(DeviceKeyAuthentication.Header, key);
        return (device, id);
    }

    private static async Task<JsonElement> UploadAsync(HttpClient device, params object[] readings)
    {
        var response = await device.PostAsJsonAsync("/api/telemetry/readings", new { readings }, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private static (int Accepted, int Duplicates, int Rejected) Counts(JsonElement answer) =>
        (answer.GetProperty("accepted").GetInt32(), answer.GetProperty("duplicates").GetInt32(),
            answer.GetProperty("rejected").GetArrayLength());

    [Fact]
    public async Task FR_T1_ABatch_IsStoredForTheInstallationItsKeyNames()
    {
        // Arrange - two devices, each with its own key
        var (device, id) = await ADeviceAsync();
        var (other, otherId) = await ADeviceAsync();

        // Act
        var answer = await UploadAsync(device, Reading(Slot(2), 1830), Reading(Slot(3), 1250));

        // Assert - stored, as kWh, against the key's own installation and nobody else's
        Counts(answer).Should().Be((2, 0, 0));
        var stored = await Context.EnergyReadings.AsNoTracking().OrderBy(r => r.RecordedAt).ToListAsync(Ct);
        stored.Should().HaveCount(2);
        stored.Should().OnlyContain(r => r.InstallationId == id);
        stored.Select(r => r.EnergyKwh).Should().Equal(1.25m, 1.83m);
        otherId.Should().NotBe(id);
        other.Dispose();
    }

    [Fact]
    public async Task FR_T2_TheSameBatchAgain_StoresNothingMore_AndCountsAsDuplicates()
    {
        // Arrange
        var (device, id) = await ADeviceAsync();
        var batch = new[] { Reading(Slot(2), 1830), Reading(Slot(3), 1250) };

        // Act - the retry a device makes when it didn't hear the first answer
        var first = await UploadAsync(device, batch);
        var again = await UploadAsync(device, batch);

        // Assert
        Counts(first).Should().Be((2, 0, 0));
        Counts(again).Should().Be((0, 2, 0));
        (await Context.EnergyReadings.CountAsync(r => r.InstallationId == id, Ct)).Should().Be(2);
    }

    [Fact]
    public async Task BR_T1_EachReadingTheRulesRefuse_IsNamedWithItsReason_AndTheRestAreStored()
    {
        // Arrange - the installation is 8.4 kW, so a slot holds at most 2.31 kWh
        var (device, _) = await ADeviceAsync();
        var good = Slot(2);

        // Act - one of each refusal, and one good reading among them
        var answer = await UploadAsync(device,
            Reading(good, 1830),
            Reading(good.AddMinutes(7), 1000),                        // not on the quarter hour
            Reading(Slot(0).AddMinutes(60), 1000),                    // a slot still to come
            Reading(Slot(2).AddDays(-31), 1000),                      // older than 30 days
            Reading(good.AddMinutes(-15), -1),                        // less than nothing
            Reading(good.AddMinutes(-30), 2_400));                    // more than the system can make

        // Assert
        Counts(answer).Should().Be((1, 0, 5));
        answer.GetProperty("rejected").EnumerateArray().Select(r => r.GetProperty("reason").GetString())
            .Should().Equal(
                "A slot starts at :00, :15, :30 or :45, exactly on the minute.",
                "This slot hasn't happened yet.",
                "Readings are accepted for the last 30 days.",
                "A system can't produce less than nothing.",
                "More than a 8.4 kW system can produce in 15 minutes (2.31 kWh).");
        (await Context.EnergyReadings.SingleAsync(Ct)).RecordedAt.Should().Be(good);
    }

    [Fact]
    public async Task BR_T2_WithoutAWorkingKey_NothingIsStored()
    {
        // Arrange - a device whose installation is deactivated after it got its key, and a client with no key at all
        var (device, id) = await ADeviceAsync();
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var noKey = CreateClient();

        // Act
        var before = await device.PostAsJsonAsync("/api/telemetry/readings", new { readings = new[] { Reading(Slot(2), 1830) } }, Ct);
        await Change(admin, HttpMethod.Post, $"/api/installations/{id}/deactivate", new { note = "Removed with the roof." });
        var after = await device.PostAsJsonAsync("/api/telemetry/readings", new { readings = new[] { Reading(Slot(3), 1830) } }, Ct);
        var never = await noKey.PostAsJsonAsync("/api/telemetry/readings", new { readings = new[] { Reading(Slot(4), 1830) } }, Ct);

        // Assert - what it sent while Active stayed; nothing after
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        await ShouldBeProblemAsync(after, HttpStatusCode.Unauthorized, "device.invalid_key");
        await ShouldBeProblemAsync(never, HttpStatusCode.Unauthorized, "device.invalid_key");
        (await Context.EnergyReadings.CountAsync(Ct)).Should().Be(1);
    }

    [Fact]
    public async Task BR_T2_AKeyLeftOnAnInstallationNoLongerActive_StoresNothing()
    {
        // Arrange - deactivating deletes the key, so this state never happens through the API; made directly, it
        // shows the service checks the status itself rather than trusting the key alone
        var (device, id) = await ADeviceAsync();
        await Context.Database.ExecuteSqlAsync($"UPDATE installations SET status = 'Deactivated' WHERE id = {id}", Ct);

        // Act
        var response = await device.PostAsJsonAsync("/api/telemetry/readings",
            new { readings = new[] { Reading(Slot(2), 1830) } }, Ct);

        // Assert
        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "device.invalid_key");
        (await Context.EnergyReadings.CountAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task FR_T1_ABatchThatIsEmptyOrTooBig_Is400()
    {
        // Arrange
        var (device, _) = await ADeviceAsync();
        var tooMany = Enumerable.Range(0, 501).Select(n => Reading(Slot(2).AddMinutes(-15 * n), 100)).ToArray();

        // Act
        var empty = await device.PostAsJsonAsync("/api/telemetry/readings", new { readings = Array.Empty<object>() }, Ct);
        var over = await device.PostAsJsonAsync("/api/telemetry/readings", new { readings = tooMany }, Ct);

        // Assert
        await ShouldBeProblemAsync(empty, HttpStatusCode.BadRequest, "validation_failed");
        await ShouldBeProblemAsync(over, HttpStatusCode.BadRequest, "validation_failed");
        (await Context.EnergyReadings.CountAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task ATimestampWithoutAZone_Is400_AsEveryRequestsIs()
    {
        var (device, _) = await ADeviceAsync();

        var response = await device.PostAsJsonAsync("/api/telemetry/readings",
            new { readings = new[] { new { slotStart = "2026-11-23T10:00:00", energyWh = 1000 } } }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task P4a_TwoBatchesAtOnce_BothStoreTheSameSlot_UntilTheFixesBranch()
    {
        // Arrange - the retry that arrives while the first batch is still being stored
        var (device, id) = await ADeviceAsync();
        var batch = Enumerable.Range(0, 20).Select(n => Reading(Slot(2).AddMinutes(-15 * n), 1000)).ToArray();
        using var start = new SemaphoreSlim(0);
        var uploads = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            await start.WaitAsync(Ct);
            return await device.PostAsJsonAsync("/api/telemetry/readings", new { readings = batch }, Ct);
        }, Ct)).ToList();

        // Act - released together
        start.Release(2);
        var responses = await Task.WhenAll(uploads);

        // Assert - both answered; nothing in the database stops one slot being stored twice, which is what P4a counts
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        var rows = await Context.EnergyReadings.CountAsync(r => r.InstallationId == id, Ct);
        var slots = await Context.EnergyReadings.Where(r => r.InstallationId == id).Select(r => r.RecordedAt).Distinct().CountAsync(Ct);
        slots.Should().Be(20);
        rows.Should().BeGreaterThanOrEqualTo(slots, "the naive build can store a slot more than once");
    }
}
