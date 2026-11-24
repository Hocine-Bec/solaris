using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Lead;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Api;

// The lead endpoints as the quote form and the staff app call them (FR-L1–L7, BR-L1–L3).
public class LeadsTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IEnumerable<string> PropertyNames(JsonElement json) => json.EnumerateObject().Select(p => p.Name);

    [Fact]
    public async Task CreateLead_New_Answers201WithTheIdOnly()
    {
        var response = await CreateClient().PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        PropertyNames(await JsonAsync(response)).Should().Equal("id");
    }

    [Fact]
    public async Task BR_L1_CreateLead_EmailWithAnOpenLead_Answers200WithThatLeadsIdOnly_AndSendsNothingNew()
    {
        // Arrange - the first request
        var client = CreateClient();
        var first = await JsonAsync(await client.PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct));

        // Act - the same person again, from another property: nothing of the first lead comes back but its id
        var response = await client.PostAsJsonAsync("/api/leads",
            ErrorResponseTests.ValidLead(email: "JANE.DOE@example.com", street: "1 Other St"), Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        PropertyNames(body).Should().Equal("id");
        body.GetProperty("id").GetInt32().Should().Be(first.GetProperty("id").GetInt32());
        (await Context.Leads.CountAsync(Ct)).Should().Be(1);
        (await Context.OutboxMessages.CountAsync(Ct)).Should().Be(2);
    }

    [Fact]
    public async Task BR_L1_TheSameFormSentFiftyTimesAtOnce_MakesOneLead_AndTheOthersAnswerItsId()
    {
        // Arrange - fifty copies of one form, as a double-clicking visitor or a retrying proxy would send them, without
        // the limit per address, as the measurement runs send them
        var client = CreateClient(environment: "Development",
            settings: new Dictionary<string, string> { ["RateLimiting:Enabled"] = "false" });
        using var start = new SemaphoreSlim(0);
        var requests = Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
        {
            await start.WaitAsync(Ct);
            return await client.PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct);
        }, Ct)).ToList();

        // Act - released together
        start.Release(requests.Count);
        var responses = await Task.WhenAll(requests);

        // Assert - the partial unique index let one insert through: one 201, forty-nine 200s, all with its id, and
        // its two emails only
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(49);
        var ids = await Task.WhenAll(responses.Select(async r => (await JsonAsync(r)).GetProperty("id").GetInt32()));
        var lead = await Context.Leads.SingleAsync(Ct);
        ids.Should().AllBeEquivalentTo(lead.Id);
        (await Context.OutboxMessages.CountAsync(Ct)).Should().Be(2);
    }

    [Fact]
    public async Task CreateLead_TwoPeopleAtOneProperty_BothSaved()
    {
        // Regression: the second answered 409 address.already_exists
        var client = CreateClient();
        (await client.PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct)).StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(email: "john.roe@example.com"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Context.Addresses.CountAsync(Ct)).Should().Be(1);
    }

    [Fact]
    public async Task GetLeads_ByStatus_ListsOnlyThoseNewestFirst_WithTheirProperty()
    {
        // Arrange - Closed in the staff app is Converted, Disqualified and Lost
        await SeedAsync(AddressFactory.CreateFakeAddress(id: 1, street: "2104 Oak Hollow Dr", city: "Round Rock", state: "TX", zipCode: "78681"));
        var statuses = new[] { LeadStatus.New, LeadStatus.Lost, LeadStatus.Contacted, LeadStatus.Converted, LeadStatus.Disqualified };
        await SeedAsync(statuses.Select((status, i) => LeadFactory.CreateFakeLead(id: i + 1, addressId: 1, status: status)).ToArray());

        // Act
        var response = await (await CreateClientAsAsync(UserRole.Admin))
            .GetAsync("/api/leads?status=Converted&status=Disqualified&status=Lost", Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("totalCount").GetInt32().Should().Be(3);
        var items = body.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("status").GetString()).Should().Equal("Disqualified", "Converted", "Lost");
        PropertyNames(items[0]).Should().Equal("id", "name", "email", "address", "propertyType", "status", "createdAt");
        items[0].GetProperty("address").GetRawText().Should()
            .Be("""{"street":"2104 Oak Hollow Dr","city":"Round Rock","state":"TX","zipCode":"78681"}""");
    }

    [Theory]
    [InlineData("Closed")]
    [InlineData("new")]
    public async Task GetLeads_UnknownStatus_Returns400ValidationFailed(string status)
    {
        var response = await (await CreateClientAsAsync(UserRole.Admin)).GetAsync($"/api/leads?status={status}", Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").GetRawText().Should().Contain("Status must be one of: New, Contacted, Converted, Disqualified, Lost");
    }

    [Fact]
    public async Task ALeadsWholePath_FromTheFormToACustomerWithAnInstallation()
    {
        // Arrange - the quote form
        var created = await JsonAsync(await CreateClient().PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct));
        var id = created.GetProperty("id").GetInt32();
        var admin = await CreateClientAsAsync(UserRole.Admin);

        // Act 1 - after a call
        var contacted = await admin.PutAsJsonAsync($"/api/leads/{id}/status", new UpdateLeadStatusRequest { Status = "Contacted" }, Ct);

        // Assert 1 - the lead as its page shows it
        contacted.StatusCode.Should().Be(HttpStatusCode.OK);
        var lead = await JsonAsync(contacted);
        lead.GetProperty("status").GetString().Should().Be("Contacted");
        lead.GetProperty("contactedAt").ValueKind.Should().Be(JsonValueKind.String);
        lead.GetProperty("next").GetRawText().Should().Be("""["Converted","Disqualified","Lost"]""");
        lead.GetProperty("customer").ValueKind.Should().Be(JsonValueKind.Null);
        lead.GetProperty("proposedTimeZone").GetRawText().Should().Be("""{"id":"America/Chicago","name":"Central"}""");

        // Act 2 - converted
        var converted = await JsonAsync(await admin.PostAsync($"/api/leads/{id}/convert", null, Ct));

        // Assert 2
        converted.GetProperty("status").GetString().Should().Be("Converted");
        converted.GetProperty("next").GetArrayLength().Should().Be(0);
        converted.GetProperty("proposedTimeZone").ValueKind.Should().Be(JsonValueKind.Null);
        var customerId = converted.GetProperty("customer").GetProperty("id").GetInt32();
        var installationId = converted.GetProperty("installationId").GetInt32();

        // Act 3 - the customer's page
        var customer = await JsonAsync(await admin.GetAsync($"/api/customers/{customerId}", Ct));

        // Assert 3
        customer.GetProperty("name").GetString().Should().Be("Jane Doe");
        customer.GetProperty("status").GetString().Should().Be("Prospect");
        customer.GetProperty("address").GetProperty("street").GetString().Should().Be("100 Congress Ave");
        var installation = customer.GetProperty("installations").EnumerateArray().Single();
        installation.GetProperty("id").GetInt32().Should().Be(installationId);
        installation.GetProperty("status").GetString().Should().Be("Survey");
        installation.GetProperty("leadId").GetInt32().Should().Be(id);
        installation.GetProperty("leadReceivedAt").GetDateTime().Should()
            .Be(await Context.Leads.Where(l => l.Id == id).Select(l => l.CreatedAt).SingleAsync(Ct));
    }

    [Fact]
    public async Task UpdateLeadStatus_Converted_Returns400_ConvertInstead()
    {
        await SeedLeadAsync(LeadStatus.Contacted);

        var response = await (await CreateClientAsAsync(UserRole.Admin)).PutAsJsonAsync("/api/leads/1/status",
            new UpdateLeadStatusRequest { Status = "Converted" }, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").GetRawText().Should().Contain("Convert the lead instead");
    }

    [Fact]
    public async Task UpdateLeadStatus_AlreadyClosed_Returns409TransitionNotAllowed()
    {
        await SeedLeadAsync(LeadStatus.Lost);

        var response = await (await CreateClientAsAsync(UserRole.Admin)).PutAsJsonAsync("/api/leads/1/status",
            new UpdateLeadStatusRequest { Status = "Contacted" }, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "lead.transition_not_allowed");
        body.GetProperty("detail").GetString().Should().Be("This lead is Lost: it can't become Contacted.");
    }

    [Fact]
    public async Task UpdateLead_OpenLead_Returns200WithTheCorrectedLead()
    {
        await SeedLeadAsync(LeadStatus.New);

        var response = await (await CreateClientAsAsync(UserRole.Admin)).PutAsJsonAsync("/api/leads/1",
            new UpdateLeadRequest { FirstName = "Dana", LastName = "Whitfield", Email = "dana@example.com", Phone = "(512) 555-0187" }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lead = await JsonAsync(response);
        (lead.GetProperty("name").GetString(), lead.GetProperty("email").GetString(), lead.GetProperty("phone").GetString())
            .Should().Be(("Dana Whitfield", "dana@example.com", "(512) 555-0187"));
    }

    [Fact]
    public async Task UpdateLead_ClosedLead_Returns409LeadClosed()
    {
        await SeedLeadAsync(LeadStatus.Disqualified);

        var response = await (await CreateClientAsAsync(UserRole.Admin)).PutAsJsonAsync("/api/leads/1",
            new UpdateLeadRequest { FirstName = "Dana", LastName = "Whitfield", Email = "dana@example.com", Phone = "(512) 555-0187" }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "lead.closed");
    }

    [Fact]
    public async Task UpdateLead_InvalidFields_Returns400WithEachField()
    {
        await SeedLeadAsync(LeadStatus.New);

        var response = await (await CreateClientAsAsync(UserRole.Admin)).PutAsJsonAsync("/api/leads/1",
            new UpdateLeadRequest { FirstName = "", LastName = "Whitfield", Email = "not an email", Phone = "call me maybe" }, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        PropertyNames(body.GetProperty("errors")).Should().BeEquivalentTo("FirstName", "Email", "Phone");
    }

    [Fact]
    public async Task GetCustomer_UnknownId_Returns404CustomerNotFound()
    {
        var response = await (await CreateClientAsAsync(UserRole.Admin)).GetAsync("/api/customers/999", Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "customer.not_found");
    }

    [Fact]
    public async Task GetCustomers_SearchByNameOrEmail_IgnoresCase_AndTakesWildcardsLiterally()
    {
        // Arrange
        await SeedAsync(
            Customer("Priya", "Raman", "priya.raman@example.com"),
            Customer("Luis", "Ortega", "luis.ortega@example.com"),
            Customer("Rachel", "Ng", "rachel_ng@example.com"));
        var admin = await CreateClientAsAsync(UserRole.Admin);
        async Task<List<string>> Names(string q) =>
            (await JsonAsync(await admin.GetAsync($"/api/customers?q={Uri.EscapeDataString(q)}", Ct)))
            .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList();

        // Act and assert - newest first
        (await Names("RAMAN")).Should().Equal("Priya Raman");
        (await Names("a r")).Should().Equal("Priya Raman");
        (await Names("@EXAMPLE")).Should().Equal("Rachel Ng", "Luis Ortega", "Priya Raman");
        (await Names("_")).Should().Equal("Rachel Ng");
        (await Names("%")).Should().BeEmpty();
    }

    private static Customer Customer(string first, string last, string email) => new()
    {
        FirstName = first, LastName = last, Email = email, NormalizedEmail = email.ToUpperInvariant(),
        PhoneNumber = "(512) 555-0100", Status = CustomerStatus.Prospect, RegistrationDate = DateTime.UtcNow
    };

    private async Task SeedLeadAsync(LeadStatus status)
    {
        await SeedAsync(AddressFactory.CreateFakeAddress(id: 1));
        await SeedAsync(LeadFactory.CreateFakeLead(id: 1, addressId: 1, status: status));
    }
}
