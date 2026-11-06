using Application.DTOs.Lead;
using Application.Errors;
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
using TestUtilities.Fakes;

namespace IntegrationTests.Services;

// LeadService with the real repositories, unit of work and PostgreSQL; only the email queue is a fake.
public class LeadServiceTests(PostgresFixture database) : IntegrationTestBase(database)
{
    static LeadServiceTests()
    {
        // The API registers the same mappings at startup.
        TypeAdapterConfig.GlobalSettings.Scan(typeof(LeadService).Assembly);
    }

    private readonly RecordingEmailQueueHandler _emails = new();

    private LeadService CreateService() => new(
        new LeadRepo(Context), new AddressRepo(Context), new CustomerRepo(Context), new UnitOfWork(Context),
        _emails);

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
        _emails.Enqueued.Should().ContainSingle().Which.LeadId.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task CreateAsync_LeadCannotBeSaved_ThrowsAndStoresNoAddressEither()
    {
        // Arrange - an email longer than its 255-character column makes the save fail
        var request = ValidRequest(email: new string('a', 250) + "@example.com");

        // Act
        var act = () => CreateService().CreateAsync(request);

        // Assert - an unexpected failure is an exception for the API's handler, not a result (ADR 3); and one
        // transaction: the address is rolled back with the lead
        await act.Should().ThrowAsync<DbUpdateException>();
        ClearTracking();
        (await Context.Leads.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await Context.Addresses.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _emails.Enqueued.Should().BeEmpty();
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
