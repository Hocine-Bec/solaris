using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Base;
using TestUtilities.Factories;

namespace IntegrationTests.Infrastructure;

public class AddressRepoTests : IntegrationTestBase
{
    private readonly AddressRepo _repo;

    public AddressRepoTests()
    {
        _repo = new AddressRepo(Context);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _repo.GetAllAsync();

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
        var result = await _repo.GetAllAsync();

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
        var result = await _repo.GetAllAsync();

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
        var result = await _repo.GetByIdAsync(1);

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
        var result = await _repo.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NegativeId_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByIdAsync(-1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ZeroId_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByIdAsync(0);

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
        var result = await _repo.GetByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.City.Should().Be("New York");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidAddress_AddsToDatabase()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress();

        // Act
        var result = await _repo.CreateAsync(address);

        // Assert
        result.Should().BeGreaterThan(0);
        var savedAddress = await Context.Addresses.FindAsync(address.Id);
        savedAddress.Should().NotBeNull();
        savedAddress!.Street.Should().Be(address.Street);
    }

    [Fact]
    public async Task CreateAsync_MultipleAddresses_AllSavedCorrectly()
    {
        // Arrange
        var address1 = AddressFactory.CreateFakeAddress(id: 1);
        var address2 = AddressFactory.CreateFakeAddress(id: 2);

        // Act
        await _repo.CreateAsync(address1);
        await _repo.CreateAsync(address2);

        // Assert
        var allAddresses = await Context.Addresses.ToListAsync();
        allAddresses.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_AddressWithAllFields_SavesAllProperties()
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
        await _repo.CreateAsync(address);
        ClearTracking();

        // Assert
        var saved = await Context.Addresses.FindAsync(address.Id);
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

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingAddress_UpdatesSuccessfully()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1, street: "Old Street");
        await SeedAsync(address);

        address.Street = "New Street";
        address.UniqueAddressHash = Address.BuildHash(
            address.Street, address.City, address.State, address.ZipCode, address.Country);

        // Act
        var result = await _repo.UpdateAsync(address);

        // Assert
        result.Should().BeTrue();
        ClearTracking();
        var updated = await Context.Addresses.FindAsync(1);
        updated!.Street.Should().Be("New Street");
    }
    
    [Fact]
    public async Task UpdateAsync_UpdatesAllModifiedFields()
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
        await _repo.UpdateAsync(address);
        ClearTracking();

        // Assert
        var updated = await Context.Addresses.FindAsync(address.Id);
        updated!.Street.Should().Be("New St");
        updated.City.Should().Be("NewCity");
        updated.State.Should().Be("CA");
        updated.ZipCode.Should().Be("99999");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingAddress_ReturnsTrue()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress();
        await SeedAsync(address);

        // Act
        var result = await _repo.DeleteAsync(address);

        // Assert
        result.Should().BeTrue();
        var deleted = await Context.Addresses.FindAsync(1);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingAddress_ReturnsFalse()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 999);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await _repo.DeleteAsync(address));
    }

    [Fact]
    public async Task DeleteAsync_OneOfMultipleAddresses_DeletesOnlySpecified()
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
        await _repo.DeleteAsync(addresses[1]);

        // Assert
        var remaining = await Context.Addresses.ToListAsync();
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
        var result = await _repo.SearchByCityAndStateAsync("Chicago");

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
        var result = await _repo.SearchByCityAndStateAsync("chicago");

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
        var result = await _repo.SearchByCityAndStateAsync("Spring");

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
        var result = await _repo.SearchByCityAndStateAsync("Springfield", "IL");

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
        var result = await _repo.SearchByCityAndStateAsync("Chicago", "il");

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
        var result = await _repo.SearchByCityAndStateAsync("NonExistentCity");

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
        var result = await _repo.SearchByCityAndStateAsync("Springfield", null);

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
        var result = await _repo.SearchByCityAndStateAsync("Springfield", "");

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
        var result = await _repo.SearchByCityAndStateAsync("o"); // Matches Boston and Chicago

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
        var result = await _repo.SearchByCityAndStateAsync("Chicago");

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
        var exists = await _repo.ExistsAsync(address.UniqueAddressHash);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ByHash_NonExistingHash_ReturnsFalse()
    {
        // Act
        var exists = await _repo.ExistsAsync("nonexistent-hash-12345");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ByHash_EmptyHash_ReturnsFalse()
    {
        // Act
        var exists = await _repo.ExistsAsync("");

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
        var exists = await _repo.ExistsAsync(addresses[1].UniqueAddressHash);

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
        await _repo.ExistsAsync(address.UniqueAddressHash);

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
        var exists = await _repo.ExistsAsync(1, address.UniqueAddressHash);

        // Assert - Should return false because we're excluding the same ID
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ByIdAndHash_DifferentIdWithSameHash_ReturnsTrue()
    {
        // Arrange
        var address1 = AddressFactory.CreateFakeAddress(
            id: 1, street: "123 Main St", city: "Chicago", state: "IL", zipCode: "60601");
        var address2 = AddressFactory.CreateFakeAddress(
            id: 2, street: "123 Main St", city: "Chicago", state: "IL", zipCode: "60601");
        
        // Force same hash for testing duplicate detection
        address2.UniqueAddressHash = address1.UniqueAddressHash;
        var addresses = new List<Address> { address1, address2 };
        await SeedAsync(addresses.ToArray());

        // Act - Check if hash exists for address with different ID
        var exists = await _repo.ExistsAsync(1, address1.UniqueAddressHash);

        // Assert - Should return true because ID 2 has the same hash
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ByIdAndHash_NonExistingHash_ReturnsFalse()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);

        // Act
        var exists = await _repo.ExistsAsync(1, "nonexistent-hash");

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
        var exists = await _repo.ExistsAsync(1, addresses[0].UniqueAddressHash);

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
        await _repo.ExistsAsync(1, address.UniqueAddressHash);

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
        var result = await _repo.GetAllAsync();

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
        var result = await _repo.SearchByCityAndStateAsync("O'Fallon");

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
        var result = await _repo.SearchByCityAndStateAsync("Springfield", "   ");

        // Assert
        result.Should().HaveCount(2); // Should treat whitespace as null/empty
    }

    [Fact]
    public async Task CreateAsync_ConcurrentCreates_BothSucceed()
    {
        // Arrange
        var address1 = AddressFactory.CreateFakeAddress(id: 1);
        var address2 = AddressFactory.CreateFakeAddress(id: 2);

        // Act
        var task1 = _repo.CreateAsync(address1);
        var task2 = _repo.CreateAsync(address2);
        await Task.WhenAll(task1, task2);

        // Assert
        var allAddresses = await Context.Addresses.ToListAsync();
        allAddresses.Should().HaveCount(2);
    }

    #endregion
}