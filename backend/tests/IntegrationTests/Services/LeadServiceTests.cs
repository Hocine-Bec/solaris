using Application.DTOs.Lead;
using Application.Errors;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Outbox;
using Infrastructure.Repositories;
using Mapster;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;
using TestUtilities.Fakes;

namespace IntegrationTests.Services;

// LeadService with the real repositories, unit of work, outbox and PostgreSQL. Rows are seeded without IDs, so the
// ones the service inserts take the next from PostgreSQL's sequence instead of colliding with them.
public class LeadServiceTests(PostgresFixture database) : IntegrationTestBase(database)
{
    static LeadServiceTests()
    {
        // The API registers the same mappings at startup.
        TypeAdapterConfig.GlobalSettings.Scan(typeof(LeadService).Assembly);
    }

    private static readonly ManualClock Clock = new(new DateTimeOffset(2026, 11, 20, 9, 30, 0, TimeSpan.Zero));
    private static readonly DateTime Earlier = new(2026, 11, 19, 16, 5, 0, DateTimeKind.Utc);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private LeadService CreateService(AppDbContext? context = null)
    {
        context ??= Context;
        return new LeadService(new LeadRepo(context), new AddressRepo(context), new CustomerRepo(context),
            new InstallationRepo(context), new UnitOfWork(context), new OutboxLeadNotifier(context, Clock), Clock);
    }

    private static CreateLeadRequest ValidRequest(string email = "jane.doe@example.com", string street = "100 Congress Ave") => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = email,
        PhoneNumber = "+1 512 555 0100",
        PropertyStreet = street,
        PropertyCity = "Austin",
        PropertyState = "TX",
        PropertyZipCode = "78701",
        PropertyCountry = "US",
        PropertyLatitude = 30.264m,
        PropertyLongitude = -97.743m,
        PropertyType = "House",
        IsPropertyOwner = true,
        MonthlyBillRange = "100-200",
        BestTimeToContact = "Morning",
        Notes = "We'd like a battery too."
    };

    private async Task<Lead> SeedLeadAsync(LeadStatus status, string email = "dana.whitfield@example.com",
        string state = "TX")
    {
        var address = AddressFactory.CreateFakeAddress(state: state);
        address.Id = 0;
        var lead = LeadFactory.CreateFakeLead(id: 0, status: status, email: email, notes: "Their own words.");
        lead.Address = address;
        lead.ContactedAt = status == LeadStatus.New ? null : Earlier;
        lead.ClosedAt = null;
        lead.CustomerId = null;
        await SeedAsync(lead);
        return lead;
    }

    private int? _adminId;

    /// <summary>The Admin who converts: an installation's history names who made each change.</summary>
    private async Task<int> AdminIdAsync()
    {
        if (_adminId is { } id)
            return id;
        var admin = new User
        {
            Name = "Rosa Castillo", Email = "rosa@solaris.test", NormalizedEmail = "ROSA@SOLARIS.TEST", PasswordHash = "unused",
            Role = UserRole.Admin, IsActive = true, CreatedAt = Earlier
        };
        await SeedAsync(admin);
        return (_adminId = admin.Id).Value;
    }

    private async Task<Customer> SeedCustomerAsync(string email)
    {
        var customer = new Customer
        {
            FirstName = "Luis", LastName = "Ortega", Email = email, NormalizedEmail = Emails.Normalize(email),
            PhoneNumber = "(520) 555-0119", Status = CustomerStatus.Prospect, RegistrationDate = Earlier
        };
        await SeedAsync(customer);
        return customer;
    }

    #region Create

    [Fact]
    public async Task CreateAsync_NewEmail_SavesTheLeadAndItsTwoNotificationsDueNow()
    {
        // Act
        var created = await CreateService().CreateAsync(ValidRequest());

        // Assert - BR-N1: saved with the lead, for the dispatcher to send
        created.IsNew.Should().BeTrue();
        ClearTracking();
        var lead = await Context.Leads.Include(l => l.Address).SingleAsync(Ct);
        lead.Id.Should().Be(created.Id);
        (lead.FullName, lead.Status, lead.CreatedAt, lead.Address.Street).Should()
            .Be(("Jane Doe", LeadStatus.New, Clock.UtcNow, "100 Congress Ave"));
        var messages = await Context.OutboxMessages.AsNoTracking().OrderBy(m => m.Type).ToListAsync(Ct);
        messages.Select(m => (m.Type, m.LeadId, m.Status, m.Attempts, m.NextAttemptAt)).Should().Equal(
            (OutboxMessageType.LeadWelcome, created.Id, OutboxMessageStatus.Pending, 0, Clock.UtcNow),
            (OutboxMessageType.SalesNewLeadAlert, created.Id, OutboxMessageStatus.Pending, 0, Clock.UtcNow));
    }

    [Fact]
    public async Task CreateAsync_WithoutCoordinates_StoresTheAddressWithoutThem()
    {
        // Arrange - a typed address, no suggestion picked: no coordinates, and none made up
        var request = ValidRequest();
        request.PropertyLatitude = null;
        request.PropertyLongitude = null;

        // Act
        await CreateService().CreateAsync(request);

        // Assert
        ClearTracking();
        var address = await Context.Addresses.SingleAsync(Ct);
        address.Latitude.Should().BeNull();
        address.Longitude.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_LeadCannotBeSaved_ThrowsAndStoresNoAddressEither()
    {
        // Arrange - an email longer than its 255-character column makes the save fail, after the address is inserted
        var request = ValidRequest(email: new string('a', 250) + "@example.com");

        // Act
        var act = () => CreateService().CreateAsync(request);

        // Assert - an unexpected failure is an exception for the API's handler, not a result (ADR 3); and one
        // transaction: the address and the notifications are rolled back with the lead (BR-N1)
        await act.Should().ThrowAsync<DbUpdateException>();
        ClearTracking();
        (await Context.Leads.CountAsync(Ct)).Should().Be(0);
        (await Context.Addresses.CountAsync(Ct)).Should().Be(0);
        (await Context.OutboxMessages.CountAsync(Ct)).Should().Be(0);
    }

    [Theory]
    [InlineData(LeadStatus.New)]
    [InlineData(LeadStatus.Contacted)]
    public async Task BR_L1_OpenLeadWithThisEmail_ReturnsItAndSavesNothingNew(LeadStatus status)
    {
        // Arrange - the same email, typed with another case and spaces around it
        var open = await SeedLeadAsync(status, email: "jane.doe@example.com");

        // Act
        var created = await CreateService().CreateAsync(ValidRequest(email: " Jane.Doe@Example.com "));

        // Assert - no new lead, no emails
        created.Should().Be(new LeadCreated(open.Id, IsNew: false));
        ClearTracking();
        (await Context.Leads.CountAsync(Ct)).Should().Be(1);
        (await Context.OutboxMessages.CountAsync(Ct)).Should().Be(0);
    }

    [Theory]
    [InlineData(LeadStatus.Converted)]
    [InlineData(LeadStatus.Disqualified)]
    [InlineData(LeadStatus.Lost)]
    public async Task BR_L1_ClosedLeadWithThisEmail_AllowsANewLead(LeadStatus status)
    {
        // Arrange
        var closed = await SeedLeadAsync(status, email: "jane.doe@example.com");

        // Act
        var created = await CreateService().CreateAsync(ValidRequest(email: "jane.doe@example.com"));

        // Assert
        created.IsNew.Should().BeTrue();
        created.Id.Should().NotBe(closed.Id);
        ClearTracking();
        (await Context.OutboxMessages.CountAsync(m => m.LeadId == created.Id, Ct)).Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_SecondLeadAtTheSameProperty_SharesTheStoredAddress()
    {
        // Arrange - regression: the second lead was refused with 409 address.already_exists, though BR-L1 allows it
        var first = await CreateService().CreateAsync(ValidRequest(email: "jane.doe@example.com"));

        // Act
        var second = await CreateService().CreateAsync(ValidRequest(email: "john.roe@example.com"));

        // Assert
        second.IsNew.Should().BeTrue();
        ClearTracking();
        (await Context.Addresses.CountAsync(Ct)).Should().Be(1);
        var addressIds = await Context.Leads.Select(l => l.AddressId).Distinct().ToListAsync(Ct);
        addressIds.Should().ContainSingle();
        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task CreateAsync_TenRequestsForOnePropertyAtOnce_ShareOneAddressRow()
    {
        // Arrange - ten people (ten emails) at one address, each request on its own context, as the API runs them
        var contexts = Enumerable.Range(0, 10).Select(_ => CreateContext()).ToList();
        using var start = new SemaphoreSlim(0);
        var requests = contexts.Select((context, i) => Task.Run(async () =>
        {
            await start.WaitAsync(Ct);
            return await CreateService(context).CreateAsync(ValidRequest(email: $"person{i}@example.com"));
        }, Ct)).ToList();

        // Act - released together
        start.Release(contexts.Count);
        var created = await Task.WhenAll(requests);

        // Assert - every lead saved; the unique index decided which request inserted the address, the others read it
        created.Should().OnlyContain(c => c.IsNew);
        (await Context.Addresses.CountAsync(Ct)).Should().Be(1);
        (await Context.Leads.CountAsync(Ct)).Should().Be(10);
        foreach (var context in contexts)
            await context.DisposeAsync();
    }

    #endregion

    #region Status

    [Fact]
    public async Task BR_L2_MarkContacted_RecordsTheServersTime_AndKeepsTheVisitorsNote()
    {
        // Arrange - regression: the time came from the request, and the office's text replaced the visitor's note
        var lead = await SeedLeadAsync(LeadStatus.New);

        // Act
        var result = await CreateService().UpdateStatusAsync(lead.Id, new UpdateLeadStatusRequest { Status = "Contacted" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Contacted");
        result.Value.Next.Should().Equal("Converted", "Disqualified", "Lost");
        ClearTracking();
        var saved = await Context.Leads.SingleAsync(Ct);
        (saved.Status, saved.ContactedAt, saved.ClosedAt, saved.Notes).Should()
            .Be((LeadStatus.Contacted, Clock.UtcNow, (DateTime?)null, "Their own words."));
    }

    [Theory]
    [InlineData(LeadStatus.Lost, "Contacted", "This lead is Lost: it can't become Contacted.")]
    [InlineData(LeadStatus.Converted, "Disqualified", "This lead is Converted: it can't become Disqualified.")]
    [InlineData(LeadStatus.Contacted, "Contacted", "This lead is Contacted: it can't become Contacted.")]
    public async Task BR_L2_MoveNotAllowedNow_IsRefusedAndChangesNothing(LeadStatus status, string to, string message)
    {
        // Arrange - for example another Admin closed it while this one's page was open
        var lead = await SeedLeadAsync(status);

        // Act
        var result = await CreateService().UpdateStatusAsync(lead.Id, new UpdateLeadStatusRequest { Status = to });

        // Assert
        result.Error!.Code.Should().Be("lead.transition_not_allowed");
        result.Error.Message.Should().Be(message);
        ClearTracking();
        (await Context.Leads.SingleAsync(Ct)).Status.Should().Be(status);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownLead_ReturnsNotFound()
    {
        var result = await CreateService().UpdateStatusAsync(999, new UpdateLeadStatusRequest { Status = "Lost" });

        result.Error.Should().Be(LeadErrors.NotFound(999));
    }

    #endregion

    #region Convert

    [Fact]
    public async Task BR_L3_ContactedLead_BecomesACustomerAndAnInstallationAtSurvey()
    {
        // Arrange
        var lead = await SeedLeadAsync(LeadStatus.Contacted, state: "TX");

        // Act
        var result = await CreateService().ConvertAsync(lead.Id, await AdminIdAsync());

        // Assert - the customer from the lead; the installation at the lead's property, in Texas's proposed zone
        result.IsSuccess.Should().BeTrue();
        ClearTracking();
        var customer = await Context.Customers.SingleAsync(Ct);
        (customer.FullName, customer.Email, customer.NormalizedEmail, customer.PhoneNumber, customer.Status,
                customer.RegistrationDate, customer.ContactAddressId).Should()
            .Be((lead.FullName, lead.Email, Emails.Normalize(lead.Email), lead.PhoneNumber, CustomerStatus.Prospect,
                Clock.UtcNow, (int?)lead.AddressId));
        var installation = await Context.Installations.SingleAsync(Ct);
        (installation.CustomerId, installation.AddressId, installation.LeadId, installation.Status,
                installation.TimeZone, installation.CreatedAt).Should()
            .Be((customer.Id, lead.AddressId, lead.Id, InstallationStatus.Survey, "America/Chicago", Clock.UtcNow));
        var converted = await Context.Leads.SingleAsync(Ct);
        (converted.Status, converted.ClosedAt, converted.CustomerId, converted.ContactedAt, converted.Notes).Should()
            .Be((LeadStatus.Converted, (DateTime?)Clock.UtcNow, (int?)customer.Id, (DateTime?)Earlier, "Their own words."));
        (result.Value.Customer!.Id, result.Value.InstallationId, result.Value.Next.Count).Should()
            .Be((customer.Id, (int?)installation.Id, 0));
    }

    [Fact]
    public async Task BR_L3_CustomerWithThisEmail_IsReused_WithTheirDetailsUnchanged()
    {
        // Arrange - a customer since an earlier conversion; this lead typed the email with another case
        var existing = await SeedCustomerAsync("luis.ortega@example.com");
        var lead = await SeedLeadAsync(LeadStatus.Contacted, email: "Luis.Ortega@example.com");

        // Act
        var result = await CreateService().ConvertAsync(lead.Id, await AdminIdAsync());

        // Assert
        result.IsSuccess.Should().BeTrue();
        ClearTracking();
        var customer = await Context.Customers.SingleAsync(Ct);
        (customer.Id, customer.FirstName, customer.Email, customer.PhoneNumber, customer.RegistrationDate).Should()
            .Be((existing.Id, "Luis", "luis.ortega@example.com", "(520) 555-0119", Earlier));
        (await Context.Installations.SingleAsync(Ct)).CustomerId.Should().Be(existing.Id);
        (await Context.Leads.SingleAsync(Ct)).CustomerId.Should().Be(existing.Id);
    }

    [Theory]
    [InlineData(LeadStatus.New, "This lead is New: only a Contacted lead can be converted.")]
    [InlineData(LeadStatus.Converted, "This lead is Converted: only a Contacted lead can be converted.")]
    [InlineData(LeadStatus.Lost, "This lead is Lost: only a Contacted lead can be converted.")]
    public async Task BR_L3_LeadNotContacted_IsRefusedAndNothingIsCreated(LeadStatus status, string message)
    {
        // Arrange
        var lead = await SeedLeadAsync(status);

        // Act
        var result = await CreateService().ConvertAsync(lead.Id, await AdminIdAsync());

        // Assert
        result.Error.Should().Be(LeadErrors.TransitionNotAllowed(status, LeadStatus.Converted));
        result.Error!.Message.Should().Be(message);
        ClearTracking();
        (await Context.Customers.CountAsync(Ct)).Should().Be(0);
        (await Context.Installations.CountAsync(Ct)).Should().Be(0);
    }

    [Fact]
    public async Task BR_L3_ConversionFailsHalfway_SavesNothing()
    {
        // Arrange - a state no zone is proposed for (the form refuses it; this lead was stored before that check)
        var lead = await SeedLeadAsync(LeadStatus.Contacted, state: "XX");

        // Act - it fails after the customer is inserted
        var adminId = await AdminIdAsync();
        var act = () => CreateService().ConvertAsync(lead.Id, adminId);

        // Assert - one transaction: the customer is rolled back, the lead is still Contacted
        await act.Should().ThrowAsync<ArgumentException>();
        ClearTracking();
        (await Context.Customers.CountAsync(Ct)).Should().Be(0);
        (await Context.Installations.CountAsync(Ct)).Should().Be(0);
        (await Context.Leads.SingleAsync(Ct)).Status.Should().Be(LeadStatus.Contacted);
    }

    [Fact]
    public async Task BR_L3_TwoLeadsWithOneEmailConvertedAtOnce_MakeOneCustomer()
    {
        // Arrange - two leads with one email (the naive BR-L1 check lets a second one through when both requests are
        // sent at the same moment), converted by two Admins at once
        var first = await SeedLeadAsync(LeadStatus.Contacted, email: "evan.price@example.com");
        var second = await SeedLeadAsync(LeadStatus.Contacted, email: "EVAN.PRICE@example.com");
        var adminId = await AdminIdAsync();
        await using var context1 = CreateContext();
        await using var context2 = CreateContext();
        using var start = new SemaphoreSlim(0);
        Task<Application.Common.Models.Result<LeadResponse>> Convert(AppDbContext context, int id) => Task.Run(async () =>
        {
            await start.WaitAsync(Ct);
            return await CreateService(context).ConvertAsync(id, adminId);
        }, Ct);
        var conversions = new[] { Convert(context1, first.Id), Convert(context2, second.Id) };

        // Act - released together
        start.Release(2);
        var results = await Task.WhenAll(conversions);

        // Assert - both converted, into the one customer the unique index allowed
        results.Should().OnlyContain(r => r.IsSuccess);
        (await Context.Customers.CountAsync(Ct)).Should().Be(1);
        var customerId = await Context.Customers.Select(c => c.Id).SingleAsync(Ct);
        (await Context.Installations.Select(i => i.CustomerId).ToListAsync(Ct)).Should().Equal(customerId, customerId);
    }

    #endregion

    #region Edit and read

    [Fact]
    public async Task FR_L7_OpenLead_GetsItsCorrectedContactDetails_AndKeepsTheRest()
    {
        // Arrange
        var lead = await SeedLeadAsync(LeadStatus.Contacted);

        // Act
        var result = await CreateService().UpdateAsync(lead.Id, new UpdateLeadRequest
        {
            FirstName = " Dana ", LastName = "Whitfeld", Email = "dana.w@example.com", Phone = "(512) 555-0188"
        });

        // Assert - trimmed; the property, the note and the status as they were
        result.IsSuccess.Should().BeTrue();
        ClearTracking();
        var saved = await Context.Leads.SingleAsync(Ct);
        (saved.FirstName, saved.LastName, saved.Email, saved.PhoneNumber, saved.Notes, saved.AddressId, saved.Status)
            .Should().Be(("Dana", "Whitfeld", "dana.w@example.com", "(512) 555-0188", "Their own words.", lead.AddressId,
                LeadStatus.Contacted));
    }

    [Theory]
    [InlineData(LeadStatus.Converted)]
    [InlineData(LeadStatus.Disqualified)]
    [InlineData(LeadStatus.Lost)]
    public async Task FR_L7_ClosedLead_IsNotEdited(LeadStatus status)
    {
        var lead = await SeedLeadAsync(status);

        var result = await CreateService().UpdateAsync(lead.Id, new UpdateLeadRequest
        {
            FirstName = "Dana", LastName = "Whitfield", Email = "dana.w@example.com", Phone = "(512) 555-0188"
        });

        result.Error.Should().Be(LeadErrors.Closed(status));
    }

    [Fact]
    public async Task BR_L1_EditingToAnotherOpenLeadsEmail_IsRefused_ButItsOwnEmailIsFine()
    {
        // Arrange
        await SeedLeadAsync(LeadStatus.New, email: "maya.brooks@example.com");
        var lead = await SeedLeadAsync(LeadStatus.New, email: "dana.whitfield@example.com");
        UpdateLeadRequest With(string email) => new()
            { FirstName = "Dana", LastName = "Whitfield", Email = email, Phone = "(512) 555-0188" };

        // Act
        var taken = await CreateService().UpdateAsync(lead.Id, With("MAYA.BROOKS@example.com"));
        var own = await CreateService().UpdateAsync(lead.Id, With("Dana.Whitfield@example.com"));

        // Assert
        taken.Error.Should().Be(LeadErrors.EmailTaken);
        own.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_OpenLead_NamesTheCustomerThatAlreadyHasItsEmail()
    {
        // Arrange - the customer a conversion would reuse
        var customer = await SeedCustomerAsync("luis.ortega@example.com");
        var lead = await SeedLeadAsync(LeadStatus.Contacted, email: "LUIS.ORTEGA@example.com");

        // Act
        var result = await CreateService().GetByIdAsync(lead.Id);

        // Assert
        result.Value.Customer.Should().Be(new CustomerLinkResponse(customer.Id, "Luis Ortega", Earlier));
        result.Value.InstallationId.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNotFound()
    {
        var result = await CreateService().GetByIdAsync(999);

        result.Error.Should().Be(LeadErrors.NotFound(999));
    }

    #endregion
}
