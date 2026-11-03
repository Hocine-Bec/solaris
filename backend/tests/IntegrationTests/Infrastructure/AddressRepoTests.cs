using Application.Common.Models;
using Infrastructure.Data;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Infrastructure;

public class AddressRepoTests(PostgresFixture database) : IntegrationTestBase(database)
{
    // The context is created per test, after the constructor runs, so the repository is created on use.
    private AddressRepo Repo => new(Context);

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await Repo.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithAddresses_ReturnsAllAddresses()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(id: 1, city: "Springfield", state: "IL"),
            AddressFactory.CreateFakeAddress(id: 2, city: "Springfield", state: "MA")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(addresses, options => options.ComparingByMembers<Address>());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDetachedEntities()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress();
        await SeedAsync(address);

        // Act
        var result = await Repo.GetAllAsync();

        // Assert
        var returnedAddress = result[0];
        Context.Entry(returnedAddress).State.Should().Be(EntityState.Detached);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsAddress()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);

        // Act
        var result = await Repo.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Street.Should().Be(address.Street);
        result.City.Should().Be(address.City);
        result.State.Should().Be(address.State);
        result.ZipCode.Should().Be(address.ZipCode);
        result.Country.Should().Be(address.Country);
        result.UniqueAddressHash.Should().Be(address.UniqueAddressHash);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await Repo.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NegativeId_ReturnsNull()
    {
        // Act
        var result = await Repo.GetByIdAsync(-1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ZeroId_ReturnsNull()
    {
        // Act
        var result = await Repo.GetByIdAsync(0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_MultipleAddresses_ReturnsCorrectOne()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(id: 1, city: "Chicago"),
            AddressFactory.CreateFakeAddress(id: 2, city: "New York"),
            AddressFactory.CreateFakeAddress(id: 3, city: "Los Angeles")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.GetByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.City.Should().Be("New York");
    }

    #endregion

    #region Add Tests

    [Fact]
    public async Task Add_ValidAddress_AddsToDatabase()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress();

        // Act
        Repo.Add(address);
        var result = await SaveAsync();

        // Assert
        result.Should().Be(SaveOutcome.Saved);
        var savedAddress = await Context.Addresses.FindAsync([address.Id], TestContext.Current.CancellationToken);
        savedAddress.Should().NotBeNull();
        savedAddress!.Street.Should().Be(address.Street);
    }

    [Fact]
    public async Task Add_MultipleAddresses_AllSavedCorrectly()
    {
        // Arrange
        var address1 = AddressFactory.CreateFakeAddress(id: 1);
        var address2 = AddressFactory.CreateFakeAddress(id: 2);

        // Act
        Repo.Add(address1);
        await SaveAsync();
        Repo.Add(address2);
        await SaveAsync();

        // Assert
        var allAddresses = await Context.Addresses.ToListAsync(TestContext.Current.CancellationToken);
        allAddresses.Should().HaveCount(2);
    }

    [Fact]
    public async Task Add_AddressWithAllFields_SavesAllProperties()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(
            street: "123 Test St",
            city: "TestCity",
            state: "IL",
            zipCode: "12345",
            country: "US"
        );

        // Act
        Repo.Add(address);
        await SaveAsync();
        ClearTracking();

        // Assert
        var saved = await Context.Addresses.FindAsync([address.Id], TestContext.Current.CancellationToken);
        saved.Should().NotBeNull();
        saved!.Street.Should().Be("123 Test St");
        saved.City.Should().Be("TestCity");
        saved.State.Should().Be("IL");
        saved.ZipCode.Should().Be("12345");
        saved.Country.Should().Be("US");
        saved.Latitude.Should().Be(address.Latitude);
        saved.Longitude.Should().Be(address.Longitude);
        saved.UniqueAddressHash.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingAddress_UpdatesSuccessfully()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1, street: "Old Street");
        await SeedAsync(address);

        address.Street = "New Street";
        address.UniqueAddressHash = Address.BuildHash(
            address.Street, address.City, address.State, address.ZipCode, address.Country);

        // Act
        Repo.Update(address);
        var result = await SaveAsync();

        // Assert
        result.Should().Be(SaveOutcome.Saved);
        ClearTracking();
        var updated = await Context.Addresses.FindAsync([1], TestContext.Current.CancellationToken);
        updated!.Street.Should().Be("New Street");
    }
    
    [Fact]
    public async Task Update_UpdatesAllModifiedFields()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(
            street: "Old St", city: "OldCity", state: "IL", zipCode: "11111");
        await SeedAsync(address);

        address.Street = "New St";
        address.City = "NewCity";
        address.State = "CA";
        address.ZipCode = "99999";
        address.UniqueAddressHash = Address.BuildHash(
            address.Street, address.City, address.State, address.ZipCode, address.Country);

        // Act
        Repo.Update(address);
        await SaveAsync();
        ClearTracking();

        // Assert
        var updated = await Context.Addresses.FindAsync([address.Id], TestContext.Current.CancellationToken);
        updated!.Street.Should().Be("New St");
        updated.City.Should().Be("NewCity");
        updated.State.Should().Be("CA");
        updated.ZipCode.Should().Be("99999");
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ExistingAddress_SaveSucceeds()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress();
        await SeedAsync(address);

        // Act
        Repo.Remove(address);
        var result = await SaveAsync();

        // Assert
        result.Should().Be(SaveOutcome.Saved);
        var deleted = await Context.Addresses.FindAsync([1], TestContext.Current.CancellationToken);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Remove_NonExistingAddress_SaveReturnsConflict()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 999);

        // Act & Assert
        Repo.Remove(address);
        (await SaveAsync()).Should().Be(SaveOutcome.Conflict);
    }

    [Fact]
    public async Task Remove_OneOfMultipleAddresses_DeletesOnlySpecified()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(id: 1),
            AddressFactory.CreateFakeAddress(id: 2),
            AddressFactory.CreateFakeAddress(id: 3)
        };
        await SeedAsync(addresses.ToArray());

        // Act
        Repo.Remove(addresses[1]);
        await SaveAsync();

        // Assert
        var remaining = await Context.Addresses.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(a => a.Id == 2);
        remaining.Should().Contain(a => a.Id == 1);
        remaining.Should().Contain(a => a.Id == 3);
    }

    #endregion

    #region SearchByCityAndStateAsync Tests

    [Fact]
    public async Task SearchByCityAndStateAsync_MatchingCity_ReturnsMatchingAddresses()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(id: 1,city: "Chicago", state: "IL"),
            AddressFactory.CreateFakeAddress(id: 2,city: "Springfield", state: "IL"),
            AddressFactory.CreateFakeAddress(id: 3,city: "Chicago", state: "IL")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Chicago");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.City.Should().Be("Chicago"));
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_CaseInsensitiveCity_ReturnsMatches()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(city: "Chicago");
        await SeedAsync(address);

        // Act
        var result = await Repo.SearchByCityAndStateAsync("chicago");

        // Assert
        result.Should().HaveCount(1);
        result.First().City.Should().Be("Chicago");
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_PartialCityMatch_ReturnsMatches()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(city: "Springfield"),
            AddressFactory.CreateFakeAddress(city: "Springdale"),
            AddressFactory.CreateFakeAddress(city: "Chicago")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Spring");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.City == "Springfield");
        result.Should().Contain(a => a.City == "Springdale");
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_WithState_FiltersCorrectly()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "IL"),
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "MA"),
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "IL"),
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "MO")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Springfield", "IL");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a =>
        {
            a.City.Should().Be("Springfield");
            a.State.Should().Be("IL");
        });
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_StateIsCaseInsensitive()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(city: "Chicago", state: "IL");
        await SeedAsync(address);

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Chicago", "il");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(city: "Chicago");
        await SeedAsync(address);

        // Act
        var result = await Repo.SearchByCityAndStateAsync("NonExistentCity");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_NullState_SearchesAllStates()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "IL"),
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "MA"),
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "MO")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Springfield", null);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_EmptyState_SearchesAllStates()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "IL"),
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "MA")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Springfield", "");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_ResultsOrderedByCityThenStreet()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(city: "Chicago", street: "Oak St"),
            AddressFactory.CreateFakeAddress(city: "Boston", street: "Main St"),
            AddressFactory.CreateFakeAddress(city: "Chicago", street: "Elm St"),
            AddressFactory.CreateFakeAddress(city: "Boston", street: "Park Ave")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.SearchByCityAndStateAsync("o"); // Matches Boston and Chicago

        // Assert
        result.Should().HaveCount(4);
        result[0].City.Should().Be("Boston");
        result[0].Street.Should().Be("Main St");
        result[1].City.Should().Be("Boston");
        result[1].Street.Should().Be("Park Ave");
        result[2].City.Should().Be("Chicago");
        result[2].Street.Should().Be("Elm St");
        result[3].City.Should().Be("Chicago");
        result[3].Street.Should().Be("Oak St");
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_ReturnsDetachedEntities()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(city: "Chicago");
        await SeedAsync(address);

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Chicago");

        // Assert
        var returnedAddress = result.First();
        Context.Entry(returnedAddress).State.Should().Be(EntityState.Detached);
    }

    #endregion

    #region ExistsAsync (UniqueAddressHash) Tests

    [Fact]
    public async Task ExistsAsync_ByHash_ExistingHash_ReturnsTrue()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress();
        await SeedAsync(address);

        // Act
        var exists = await Repo.ExistsAsync(address.UniqueAddressHash);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ByHash_NonExistingHash_ReturnsFalse()
    {
        // Act
        var exists = await Repo.ExistsAsync("nonexistent-hash-12345");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ByHash_EmptyHash_ReturnsFalse()
    {
        // Act
        var exists = await Repo.ExistsAsync("");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ByHash_MultipleAddresses_FindsCorrectOne()
    {
        // Arrange
        var addresses = AddressFactory.CreateFakeAddresses(3);
        await SeedAsync(addresses.ToArray());

        // Act
        var exists = await Repo.ExistsAsync(addresses[1].UniqueAddressHash);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ByHash_DoesNotTrackEntities()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress();
        await SeedAsync(address);

        // Act
        await Repo.ExistsAsync(address.UniqueAddressHash);

        // Assert
        Context.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion

    #region ExistsAsync (Id and UniqueAddressHash) Tests

    [Fact]
    public async Task ExistsAsync_ByIdAndHash_SameIdAndHash_ReturnsFalse()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);

        // Act - Check if hash exists for a different ID (excluding ID 1)
        var exists = await Repo.ExistsAsync(1, address.UniqueAddressHash);

        // Assert - Should return false because we're excluding the same ID
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ByIdAndHash_DifferentIdWithSameHash_ReturnsTrue()
    {
        // Arrange - the unique index allows only one row per hash
        var address = AddressFactory.CreateFakeAddress(
            id: 1, street: "123 Main St", city: "Chicago", state: "IL", zipCode: "60601");
        await SeedAsync(address);

        // Act - check the hash for a different address (ID 2), e.g. one being updated to this address
        var exists = await Repo.ExistsAsync(2, address.UniqueAddressHash);

        // Assert - true, because address 1 already has this hash
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ByIdAndHash_NonExistingHash_ReturnsFalse()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);

        // Act
        var exists = await Repo.ExistsAsync(1, "nonexistent-hash");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ByIdAndHash_MultipleAddressesWithUniqueHashes_ReturnsFalse()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(id: 1, street: "123 Main St"),
            AddressFactory.CreateFakeAddress(id: 2, street: "456 Oak Ave"),
            AddressFactory.CreateFakeAddress(id: 3, street: "789 Pine Rd")
        };
        await SeedAsync(addresses.ToArray());

        // Act - Check if address 1's hash exists elsewhere
        var exists = await Repo.ExistsAsync(1, addresses[0].UniqueAddressHash);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ByIdAndHash_DoesNotTrackEntities()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        
        // Act
        await Repo.ExistsAsync(1, address.UniqueAddressHash);

        // Assert
        Context.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public async Task GetAllAsync_LargeDataset_ReturnsAllRecords()
    {
        // Arrange
        var addresses = AddressFactory.CreateFakeAddresses(20);
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.GetAllAsync();

        // Assert
        result.Should().HaveCount(20);
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_SpecialCharactersInCity_HandlesCorrectly()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(city: "O'Fallon");
        await SeedAsync(address);

        // Act
        var result = await Repo.SearchByCityAndStateAsync("O'Fallon");

        // Assert
        result.Should().HaveCount(1);
        result.First().City.Should().Be("O'Fallon");
    }

    [Fact]
    public async Task SearchByCityAndStateAsync_WhitespaceInState_TreatsAsNull()
    {
        // Arrange
        var addresses = new List<Address>
        {
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "IL"),
            AddressFactory.CreateFakeAddress(city: "Springfield", state: "MA")
        };
        await SeedAsync(addresses.ToArray());

        // Act
        var result = await Repo.SearchByCityAndStateAsync("Springfield", "   ");

        // Assert
        result.Should().HaveCount(2); // Should treat whitespace as null/empty
    }

    [Fact]
    public async Task Add_ConcurrentCreates_BothSucceed()
    {
        // Arrange
        var address1 = AddressFactory.CreateFakeAddress(id: 1);
        var address2 = AddressFactory.CreateFakeAddress(id: 2);

        // Act - one context per operation, as two simultaneous requests would have
        await using var context1 = CreateContext();
        await using var context2 = CreateContext();
        new AddressRepo(context1).Add(address1);
        new AddressRepo(context2).Add(address2);
        var outcomes = await Task.WhenAll(
            new UnitOfWork(context1).SaveAsync(TestContext.Current.CancellationToken),
            new UnitOfWork(context2).SaveAsync(TestContext.Current.CancellationToken));
        outcomes.Should().AllBeEquivalentTo(SaveOutcome.Saved);

        // Assert
        var allAddresses = await Context.Addresses.ToListAsync(TestContext.Current.CancellationToken);
        allAddresses.Should().HaveCount(2);
    }

    #endregion
}