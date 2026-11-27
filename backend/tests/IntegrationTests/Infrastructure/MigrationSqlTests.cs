using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Infrastructure;

// The SQL a migration runs on data that already exists, run again here on rows made for it: what it computes must be
// what the code computes, and what it can't migrate must stop it, saying why.
public class MigrationSqlTests(PostgresFixture database) : IntegrationTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // An INSERT can't be composed over: its one row is read as a list
    private async Task<int> InsertAddressAsync(string street, string city, string hash) =>
        (await Context.Database.SqlQuery<int>($"""
            INSERT INTO addresses (street, city, state, zip_code, country, unique_address_hash)
            VALUES ({street}, {city}, 'TX', '78704', 'US', {hash}) RETURNING id AS "Value"
            """).ToListAsync(Ct)).Single();

    private async Task<string> HashOfAsync(int id) =>
        await Context.Database.SqlQuery<string>($"""SELECT unique_address_hash AS "Value" FROM addresses WHERE id = {id}""").SingleAsync(Ct);

    [Fact]
    public async Task RehashAddresses_GivesAnAddressWithWhiteSpaceTheKeyBuildHashGives_AndLeavesTheOthers()
    {
        // Arrange - one address stored with white space around and inside its fields, under its old key; one without
        var untidy = await InsertAddressAsync("  2104   Oak Hollow\tDr ", "Austin\n", "old key");
        var tidy = await InsertAddressAsync("1 Main St", "Austin", "kept as it is");

        // Act
        await Context.Database.ExecuteSqlRawAsync(EnforceOpenLeadEmailAndVersions.RehashAddressesSql, Ct);

        // Assert
        (await HashOfAsync(untidy)).Should().Be(Address.BuildHash("2104 Oak Hollow Dr", "Austin", "TX", "78704", "US"));
        (await HashOfAsync(tidy)).Should().Be("kept as it is");
    }

    [Fact]
    public async Task RehashAddresses_TwoAddressesThatBecomeOne_StopTheMigration_NamingThem()
    {
        // Arrange - the same property twice: once as typed tidily, once with a double space
        var tidy = await InsertAddressAsync("1 Main St", "Austin", Address.BuildHash("1 Main St", "Austin", "TX", "78704", "US"));
        var untidy = await InsertAddressAsync("1  Main St", "Austin", "old key");

        // Act
        var act = () => Context.Database.ExecuteSqlRawAsync(EnforceOpenLeadEmailAndVersions.RehashAddressesSql, Ct);

        // Assert - nothing changed
        (await act.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should()
            .Be($"Addresses become the same address once white space is ignored: {untidy} and {tidy}. Merge them, then migrate again.");
        (await HashOfAsync(untidy)).Should().Be("old key");
    }

    [Fact]
    public async Task StopOnSharedOpenEmails_OpenLeadsWithOneEmail_StopTheMigration_NamingThem()
    {
        // Arrange - the state the naive check allowed: two open leads with one email (and a closed one), so the index
        // the migration makes is dropped here first
        await Context.Database.ExecuteSqlRawAsync("DROP INDEX ux_leads_open_normalized_email", Ct);
        var address = AddressFactory.CreateFakeAddress();
        await SeedAsync(address);
        var leads = new[] { LeadStatus.New, LeadStatus.Contacted, LeadStatus.Lost }
            .Select(s => LeadFactory.CreateFakeLead(id: 0, email: "jane.doe@example.com", addressId: address.Id, status: s))
            .ToArray();
        await SeedAsync(leads);

        // Act
        var act = () => Context.Database.ExecuteSqlRawAsync(EnforceOpenLeadEmailAndVersions.StopOnSharedOpenEmailsSql, Ct);

        // Assert
        (await act.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should()
            .Be($"Open leads share an email (BR-L1): leads {leads[0].Id}, {leads[1].Id}. Close all but one of each, then migrate again.");
    }

    [Fact]
    public async Task DropDuplicateSlots_KeepsEachSlotsEarliestRow()
    {
        // Arrange - the state the naive table allowed: one slot stored three times, and another stored once. The
        // table has its final shape here, so the old columns are made back for the statement to work on.
        await Context.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE kept AS SELECT 1;
            DROP TABLE energy_readings;
            CREATE TABLE energy_readings (
              id bigserial PRIMARY KEY, installation_id int NOT NULL,
              recorded_at timestamptz NOT NULL, energy_kwh numeric(10,4) NOT NULL);
            INSERT INTO energy_readings (installation_id, recorded_at, energy_kwh) VALUES
              (1, '2026-11-20 18:00:00+00', 1.8300),
              (1, '2026-11-20 18:00:00+00', 9.9999),
              (1, '2026-11-20 18:00:00+00', 5.5555),
              (1, '2026-11-20 18:15:00+00', 0.4567),
              (2, '2026-11-20 18:00:00+00', 2.0000);
            """, Ct);

        // Act
        await Context.Database.ExecuteSqlRawAsync(FinalReadingSchema.DropDuplicateSlotsSql, Ct);

        // Assert - one row per installation and slot, and the one kept is the earliest of each
        var left = await Context.Database.SqlQueryRaw<decimal>(
            "SELECT energy_kwh AS \"Value\" FROM energy_readings ORDER BY installation_id, recorded_at").ToListAsync(Ct);
        left.Should().Equal(1.8300m, 0.4567m, 2.0000m);
    }
}
