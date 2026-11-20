using Application.DTOs.Lead;
using Application.Errors;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Mapster;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;
using Infrastructure.Outbox;
using TestUtilities.Fakes;

namespace IntegrationTests.Services;

// LeadService with the real repositories, unit of work, outbox and PostgreSQL.
public class LeadServiceTests(PostgresFixture database) : IntegrationTestBase(database)
{
    static LeadServiceTests()
    {
        // The API registers the same mappings at startup.
        TypeAdapterConfig.GlobalSettings.Scan(typeof(LeadService).Assembly);
    }

    private static readonly ManualClock Clock = new(new DateTimeOffset(2026, 11, 15, 9, 30, 0, TimeSpan.Zero));

    private LeadService CreateService() => new(
        new LeadRepo(Context), new AddressRepo(Context), new CustomerRepo(Context), new UnitOfWork(Context),
        new OutboxLeadNotifier(Context, Clock));

    private static CreateLeadRequest ValidRequest(string email = "jane.doe@example.com") => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = email,
        PhoneNumber = "+1 512 555 0100",
        PropertyStreet = "100 Congress Ave",
        PropertyCity = "Austin",
        PropertyState = "TX",
        PropertyZipCode = "78701",
        PropertyCountry = "US",
        PropertyLatitude = 30.264m,
        PropertyLongitude = -97.743m,
        PropertyType = "House",
        IsPropertyOwner = true,
        MonthlyBillRange = "$100-$200",
        BestTimeToContact = "Morning"
    };

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsTheCreatedLead()
    {
        // Act
        var result = await CreateService().CreateAsync(ValidRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.FullName.Should().Be("Jane Doe");
        result.Value.Email.Should().Be("jane.doe@example.com");
        result.Value.PropertyAddress.Should().StartWith("100 Congress Ave");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesTheLeadsTwoNotificationsDueNow()
    {
        // Act
        var result = await CreateService().CreateAsync(ValidRequest());

        // Assert - BR-N1: saved with the lead, for the dispatcher to send
        ClearTracking();
        var messages = await Context.OutboxMessages.AsNoTracking().OrderBy(m => m.Type)
            .ToListAsync(TestContext.Current.CancellationToken);
        messages.Select(m => (m.Type, m.LeadId, m.Status, m.Attempts, m.NextAttemptAt)).Should().Equal(
            (OutboxMessageType.LeadWelcome, result.Value.Id, OutboxMessageStatus.Pending, 0, Clock.UtcNow),
            (OutboxMessageType.SalesNewLeadAlert, result.Value.Id, OutboxMessageStatus.Pending, 0, Clock.UtcNow));
    }

    [Fact]
    public async Task CreateAsync_WithoutCoordinates_StoresTheAddressWithoutThem()
    {
        // Arrange - a typed address, no suggestion picked: no coordinates, and none made up
        var request = ValidRequest();
        request.PropertyLatitude = null;
        request.PropertyLongitude = null;

        // Act
        var result = await CreateService().CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ClearTracking();
        var address = await Context.Addresses.SingleAsync(TestContext.Current.CancellationToken);
        address.Latitude.Should().BeNull();
        address.Longitude.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_LeadCannotBeSaved_ThrowsAndStoresNoAddressEither()
    {
        // Arrange - an email longer than its 255-character column makes the save fail
        var request = ValidRequest(email: new string('a', 250) + "@example.com");

        // Act
        var act = () => CreateService().CreateAsync(request);

        // Assert - an unexpected failure is an exception for the API's handler, not a result (ADR 3); and one
        // transaction: the address and the notifications are rolled back with the lead (BR-N1)
        await act.Should().ThrowAsync<DbUpdateException>();
        ClearTracking();
        (await Context.Leads.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await Context.Addresses.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await Context.OutboxMessages.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task BR_N1_LeadRefusedByTheDatabase_SavesNoNotificationEither()
    {
        // Arrange - another request saved this address after this one checked it: the unique index refuses the lead
        var request = ValidRequest();
        await SeedAsync(new Address
        {
            Street = request.PropertyStreet, City = request.PropertyCity, State = request.PropertyState,
            ZipCode = request.PropertyZipCode, Country = request.PropertyCountry,
            UniqueAddressHash = Address.BuildHash(request.PropertyStreet, request.PropertyCity, request.PropertyState,
                request.PropertyZipCode, request.PropertyCountry)
        });
        var service = new LeadService(new LeadRepo(Context), new AddressCheckedBeforeTheOtherSave(new AddressRepo(Context)),
            new CustomerRepo(Context), new UnitOfWork(Context), new OutboxLeadNotifier(Context, Clock));

        // Act
        var result = await service.CreateAsync(request);

        // Assert - one transaction: no lead, and no notification for it
        result.Error.Should().Be(AddressErrors.AlreadyExists);
        ClearTracking();
        (await Context.Leads.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await Context.OutboxMessages.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    /// <summary>The address check as it was before the other request saved: it finds nothing.</summary>
    private sealed class AddressCheckedBeforeTheOtherSave(AddressRepo inner) : IAddressRepo
    {
        public Task<bool> ExistsAsync(string uniqueAddressHash) => Task.FromResult(false);
        public Task<Address?> GetByIdAsync(int id) => inner.GetByIdAsync(id);
        public void Add(Address entity) => inner.Add(entity);
        public void Update(Address entity) => inner.Update(entity);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNotFound()
    {
        // Act
        var result = await CreateService().GetByIdAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(LeadErrors.NotFound(999));
    }

    [Fact]
    public async Task ConvertLeadToCustomerAsync_LinksTheLeadToTheNewCustomer()
    {
        // Arrange - an existing customer first, so the new customer's ID (2) differs from the number of rows saved (1)
        var address = AddressFactory.CreateFakeAddress(id: 1);
        await SeedAsync(address);
        await SeedAsync(new Customer
        {
            FirstName = "Existing",
            LastName = "Customer",
            Email = "existing.customer@example.com",
            PhoneNumber = "512-555-0101",
            ContactAddressId = 1
        });
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1, status: LeadStatus.Contacted);
        await SeedAsync(lead);

        // Act
        var result = await CreateService().ConvertLeadToCustomerAsync(new ConvertLeadRequest { LeadId = 1 });

        // Assert
        result.IsSuccess.Should().BeTrue();
        ClearTracking();
        var converted = await Context.Leads.SingleAsync(l => l.Id == 1, TestContext.Current.CancellationToken);
        var customer = await Context.Customers.SingleAsync(c => c.Email == lead.Email, TestContext.Current.CancellationToken);
        customer.Id.Should().Be(2);
        converted.CustomerId.Should().Be(customer.Id);
        converted.Status.Should().Be(LeadStatus.Converted);
    }
}
