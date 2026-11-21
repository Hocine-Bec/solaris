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

    #region GetOrAddAsync (UniqueAddressHash) Tests

    // Addresses made by the factory carry IDs; these are cleared, so PostgreSQL's sequence gives them.
    private static Address NewAddress(string street = "2104 Oak Hollow Dr")
    {
        var address = AddressFactory.CreateFakeAddress(street: street);
        address.Id = 0;
        return address;
    }

    [Fact]
    public async Task GetOrAddAsync_NewKey_InsertsTheAddressAndReturnsItTracked()
    {
        // Arrange
        var address = NewAddress();

        // Act
        var stored = await Repo.GetOrAddAsync(address);

        // Assert
        stored.Id.Should().BeGreaterThan(0);
        (stored.Street, stored.City, stored.State, stored.ZipCode, stored.Latitude, stored.Longitude).Should()
            .Be((address.Street, address.City, address.State, address.ZipCode, address.Latitude, address.Longitude));
        Context.Entry(stored).State.Should().Be(EntityState.Unchanged);
        (await Context.Addresses.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task GetOrAddAsync_StoredKey_ReturnsTheStoredAddressAndInsertsNothing()
    {
        // Arrange - the same key, sent again with other coordinates: the stored row stays as it is (ADR 5)
        var first = NewAddress();
        await SeedAsync(first);
        var again = NewAddress();
        again.Street = first.Street; again.City = first.City; again.State = first.State; again.ZipCode = first.ZipCode;
        again.Country = first.Country; again.UniqueAddressHash = first.UniqueAddressHash;
        again.Latitude = 1m; again.Longitude = 2m;

        // Act
        var stored = await Repo.GetOrAddAsync(again);

        // Assert
        stored.Id.Should().Be(first.Id);
        stored.Latitude.Should().Be(first.Latitude);
        (await Context.Addresses.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task GetOrAddAsync_ManyAtOnceForOneKey_StoreOneRowAndAllReturnIt()
    {
        // Arrange - one context per request, released together
        var address = NewAddress();
        var contexts = Enumerable.Range(0, 8).Select(_ => CreateContext()).ToList();
        using var start = new SemaphoreSlim(0);
        var calls = contexts.Select(context => Task.Run(async () =>
        {
            await start.WaitAsync(TestContext.Current.CancellationToken);
            return (await new AddressRepo(context).GetOrAddAsync(address)).Id;
        }, TestContext.Current.CancellationToken)).ToList();

        // Act
        start.Release(contexts.Count);
        var ids = await Task.WhenAll(calls);

        // Assert
        ids.Distinct().Should().ContainSingle();
        (await Context.Addresses.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        foreach (var context in contexts)
            await context.DisposeAsync();
    }

    #endregion

    #region Edge Cases and Boundary Tests

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