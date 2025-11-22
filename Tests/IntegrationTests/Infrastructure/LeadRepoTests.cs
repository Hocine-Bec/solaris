using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Base;
using TestUtilities.Factories;

namespace IntegrationTests.Infrastructure;

public class LeadRepoTests : IntegrationTestBase
{
    private readonly LeadRepo _repo;

    public LeadRepoTests()
    {
        _repo = new LeadRepo(Context);
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
    public async Task GetAllAsync_WithLeads_ReturnsAllLeads()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var leads = new List<Lead>
        {
            LeadFactory.CreateFakeLead(id: 1, addressId: 1, firstName: "John"),
            LeadFactory.CreateFakeLead(id: 2, addressId: 1, firstName: "Jane")
        };
        await SeedAsync(address);
        await SeedAsync(leads.ToArray());

        // Act
        var result = await _repo.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_IncludesAddress()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1, city: "Chicago");
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        await SeedAsync(address);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Address.Should().NotBeNull();
        result[0].Address.City.Should().Be("Chicago");
    }

    [Fact]
    public async Task GetAllAsync_IncludesCustomer_WhenConverted()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var customer = new Customer
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "123-456-7890",
            ContactAddressId = 1
        };
        var lead = LeadFactory.CreateFakeLead(
            id: 1, 
            addressId: 1, 
            customerId: 1,
            status: LeadStatus.Converted);
        
        await SeedAsync(address);
        await SeedAsync(customer);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Customer.Should().NotBeNull();
        result[0].Customer!.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDetachedEntities()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        await SeedAsync(address);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetAllAsync();

        // Assert
        var returnedLead = result[0];
        Context.Entry(returnedLead).State.Should().Be(EntityState.Detached);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsLead()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(
            id: 1, 
            addressId: 1,
            firstName: "John",
            lastName: "Doe",
            email: "john@example.com");
        await SeedAsync(address);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john@example.com");
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
    public async Task GetByIdAsync_IncludesAddress()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1, city: "New York");
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        await SeedAsync(address);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Address.Should().NotBeNull();
        result.Address.City.Should().Be("New York");
    }

    [Fact]
    public async Task GetByIdAsync_IncludesCustomer_WhenConverted()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var customer = new Customer
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            PhoneNumber = "987-654-3210",
            ContactAddressId = 1
        };
        var lead = LeadFactory.CreateFakeLead(
            id: 1, 
            addressId: 1, 
            customerId: 1,
            status: LeadStatus.Converted);
        
        await SeedAsync(address);
        await SeedAsync(customer);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Customer.Should().NotBeNull();
        result.Customer!.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task GetByIdAsync_MultipleLeads_ReturnsCorrectOne()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var leads = new List<Lead>
        {
            LeadFactory.CreateFakeLead(id: 1, addressId: 1, firstName: "Alice"),
            LeadFactory.CreateFakeLead(id: 2, addressId: 1, firstName: "Bob"),
            LeadFactory.CreateFakeLead(id: 3, addressId: 1, firstName: "Charlie")
        };
        await SeedAsync(address);
        await SeedAsync(leads.ToArray());

        // Act
        var result = await _repo.GetByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.FirstName.Should().Be("Bob");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetachedEntity()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        await SeedAsync(address);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetByIdAsync(1);

        // Assert
        Context.Entry(result!).State.Should().Be(EntityState.Detached);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidLead_AddsToDatabase()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        
        var lead = LeadFactory.CreateFakeLead(addressId: 1);

        // Act
        var result = await _repo.CreateAsync(lead);

        // Assert
        result.Should().BeGreaterThan(0);
        var savedLead = await Context.Leads.FindAsync(lead.Id);
        savedLead.Should().NotBeNull();
        savedLead!.FirstName.Should().Be(lead.FirstName);
    }

    [Fact]
    public async Task CreateAsync_MultipleLeads_AllSavedCorrectly()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        
        var lead1 = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        var lead2 = LeadFactory.CreateFakeLead(id: 2, addressId: 1);

        // Act
        await _repo.CreateAsync(lead1);
        await _repo.CreateAsync(lead2);

        // Assert
        var allLeads = await Context.Leads.ToListAsync();
        allLeads.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_LeadWithAllFields_SavesAllProperties()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        
        var createdAt = DateTime.UtcNow;
        var lead = LeadFactory.CreateFakeLead(
            addressId: 1,
            firstName: "Test",
            lastName: "User",
            email: "test@example.com",
            phoneNumber: "555-1234",
            propertyType: PropertyType.Commercial,
            isPropertyOwner: true,
            status: LeadStatus.New,
            createdAt: createdAt,
            monthlyBillRange: "$300+",
            bestTimeToContact: "Evening",
            notes: "Test notes"
        );

        // Act
        await _repo.CreateAsync(lead);
        ClearTracking();

        // Assert
        var saved = await Context.Leads.FindAsync(lead.Id);
        saved.Should().NotBeNull();
        saved!.FirstName.Should().Be("Test");
        saved.LastName.Should().Be("User");
        saved.Email.Should().Be("test@example.com");
        saved.PhoneNumber.Should().Be("555-1234");
        saved.PropertyType.Should().Be(PropertyType.Commercial);
        saved.IsPropertyOwner.Should().BeTrue();
        saved.Status.Should().Be(LeadStatus.New);
        saved.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
        saved.MonthlyBillRange.Should().Be("$300+");
        saved.BestTimeToContact.Should().Be("Evening");
        saved.Notes.Should().Be("Test notes");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingLead_UpdatesSuccessfully()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(
            id: 1, 
            addressId: 1, 
            status: LeadStatus.New);
        await SeedAsync(address);
        await SeedAsync(lead);

        lead.Status = LeadStatus.Contacted;
        lead.ContactedAt = DateTime.UtcNow;

        // Act
        var result = await _repo.UpdateAsync(lead);

        // Assert
        result.Should().BeTrue();
        ClearTracking();
        var updated = await Context.Leads.FindAsync(1);
        updated!.Status.Should().Be(LeadStatus.Contacted);
        updated.ContactedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllModifiedFields()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(
            id: 1,
            addressId: 1,
            firstName: "Old",
            lastName: "Name",
            status: LeadStatus.New);
        await SeedAsync(address);
        await SeedAsync(lead);

        lead.FirstName = "New";
        lead.LastName = "Person";
        lead.Status = LeadStatus.Contacted;
        lead.Notes = "Updated notes";

        // Act
        await _repo.UpdateAsync(lead);
        ClearTracking();

        // Assert
        var updated = await Context.Leads.FindAsync(1);
        updated!.FirstName.Should().Be("New");
        updated.LastName.Should().Be("Person");
        updated.Status.Should().Be(LeadStatus.Contacted);
        updated.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateAsync_ConvertLead_SetsConvertedAtAndCustomerId()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(
            id: 1,
            addressId: 1,
            status: LeadStatus.Contacted);
        await SeedAsync(address);
        await SeedAsync(lead);

        var convertedAt = DateTime.UtcNow;
        lead.Status = LeadStatus.Converted;
        lead.ConvertedAt = convertedAt;
        lead.CustomerId = 100;

        // Act
        await _repo.UpdateAsync(lead);
        ClearTracking();

        // Assert
        var updated = await Context.Leads.FindAsync(1);
        updated!.Status.Should().Be(LeadStatus.Converted);
        updated.ConvertedAt.Should().BeCloseTo(convertedAt, TimeSpan.FromSeconds(1));
        updated.CustomerId.Should().Be(100);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingLead_ReturnsTrue()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        await SeedAsync(address);
        await SeedAsync(lead);

        // Act
        var result = await _repo.DeleteAsync(lead);

        // Assert
        result.Should().BeTrue();
        var deleted = await Context.Leads.FindAsync(1);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingLead_ThrowsException()
    {
        // Arrange
        var lead = LeadFactory.CreateFakeLead(id: 999, addressId: 1);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await _repo.DeleteAsync(lead));
    }

    [Fact]
    public async Task DeleteAsync_OneOfMultipleLeads_DeletesOnlySpecified()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var leads = new List<Lead>
        {
            LeadFactory.CreateFakeLead(id: 1, addressId: 1),
            LeadFactory.CreateFakeLead(id: 2, addressId: 1),
            LeadFactory.CreateFakeLead(id: 3, addressId: 1)
        };
        await SeedAsync(address);
        await SeedAsync(leads.ToArray());

        // Act
        await _repo.DeleteAsync(leads[1]);

        // Assert
        var remaining = await Context.Leads.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(l => l.Id == 2);
        remaining.Should().Contain(l => l.Id == 1);
        remaining.Should().Contain(l => l.Id == 3);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public async Task GetAllAsync_LargeDataset_ReturnsAllRecords()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        
        var leads = new List<Lead>();
        for (int i = 1; i <= 50; i++)
        {
            leads.Add(LeadFactory.CreateFakeLead(id: i, addressId: 1));
        }
        await SeedAsync(leads.ToArray());

        // Act
        var result = await _repo.GetAllAsync();

        // Assert
        result.Should().HaveCount(50);
    }

    [Fact]
    public async Task CreateAsync_LeadWithDifferentStatuses_SavesCorrectly()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        
        var leads = new List<Lead>
        {
            LeadFactory.CreateFakeLead(id: 1, addressId: 1, status: LeadStatus.New),
            LeadFactory.CreateFakeLead(id: 2, addressId: 1, status: LeadStatus.Contacted),
            LeadFactory.CreateFakeLead(id: 3, addressId: 1, status: LeadStatus.Converted),
            LeadFactory.CreateFakeLead(id: 4, addressId: 1, status: LeadStatus.Disqualified),
            LeadFactory.CreateFakeLead(id: 5, addressId: 1, status: LeadStatus.Lost)
        };

        // Act
        foreach (var lead in leads)
        {
            await _repo.CreateAsync(lead);
        }

        // Assert
        var allLeads = await Context.Leads.ToListAsync();
        allLeads.Should().HaveCount(5);
        allLeads.Should().Contain(l => l.Status == LeadStatus.New);
        allLeads.Should().Contain(l => l.Status == LeadStatus.Contacted);
        allLeads.Should().Contain(l => l.Status == LeadStatus.Converted);
        allLeads.Should().Contain(l => l.Status == LeadStatus.Disqualified);
        allLeads.Should().Contain(l => l.Status == LeadStatus.Lost);
    }

    [Fact]
    public async Task GetByIdAsync_LeadWithNullableFields_HandlesCorrectly()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        var lead = LeadFactory.CreateFakeLead(
            id: 1,
            addressId: 1,
            contactedAt: null,
            convertedAt: null,
            notes: null,
            customerId: null);
        
        // Explicitly set nullable fields to null to ensure they are null
        lead.ContactedAt = null;
        lead.ConvertedAt = null;
        lead.Notes = null;
        lead.CustomerId = null;
        
        await SeedAsync(address);
        await SeedAsync(lead);

        // Act
        var result = await _repo.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.ContactedAt.Should().BeNull();
        result.ConvertedAt.Should().BeNull();
        result.Notes.Should().BeNull();
        result.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ConcurrentCreates_BothSucceed()
    {
        // Arrange
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        
        var lead1 = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        var lead2 = LeadFactory.CreateFakeLead(id: 2, addressId: 1);

        // Act
        var task1 = _repo.CreateAsync(lead1);
        var task2 = _repo.CreateAsync(lead2);
        await Task.WhenAll(task1, task2);

        // Assert
        var allLeads = await Context.Leads.ToListAsync();
        allLeads.Should().HaveCount(2);
    }

    #endregion
}
