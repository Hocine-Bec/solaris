using Application.Common.Models;
using FluentAssertions;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Infrastructure;

public class UnitOfWorkTests(PostgresFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task SaveAsync_UniqueIndexRejectsTheWrite_ReturnsDuplicate()
    {
        // Arrange
        var existing = AddressFactory.CreateFakeAddress();
        await SeedAsync(existing);
        var copy = AddressFactory.CreateFakeAddress();
        copy.UniqueAddressHash = existing.UniqueAddressHash;
        new AddressRepo(Context).Add(copy);

        // Act
        var outcome = await SaveAsync();

        // Assert
        outcome.Should().Be(SaveOutcome.Duplicate);
    }

    [Fact]
    public async Task SaveAsync_RowDeletedAfterItWasRead_ReturnsConflict()
    {
        // Arrange - this context reads the address, then another connection deletes it
        await SeedAsync(AddressFactory.CreateFakeAddress());
        var address = await Context.Addresses.SingleAsync(TestContext.Current.CancellationToken);
        await using (var other = CreateContext())
        {
            other.Addresses.Remove(await other.Addresses.SingleAsync(TestContext.Current.CancellationToken));
            await other.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        address.City = "Elsewhere";

        // Act
        var outcome = await SaveAsync();

        // Assert
        outcome.Should().Be(SaveOutcome.Conflict);
    }
}
