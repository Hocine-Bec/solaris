using System.Net;
using System.Net.Http.Json;
using System.Text;
using Application.Common.Models;
using Application.DTOs.Common;
using Application.DTOs.Lead;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Api;

// Every error response is a ProblemDetails with a stable code and a traceId (ADR 3).
public class ErrorResponseTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal static CreateLeadRequest ValidLead(string email = "jane.doe@example.com",
        string street = "100 Congress Ave") => new()
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
        MonthlyBillRange = "$100-$200",
        BestTimeToContact = "Morning"
    };

    [Fact]
    public async Task GetLead_UnknownId_Returns404LeadNotFound()
    {
        var response = await CreateClient().GetAsync("/api/leads/999", Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "lead.not_found");
        body.GetProperty("detail").GetString().Should().Be("Lead with ID 999 not found.");
    }

    [Fact]
    public async Task GetAddress_UnknownId_Returns404AddressNotFound()
    {
        var response = await CreateClient().GetAsync("/api/addresses/999", Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "address.not_found");
    }

    [Fact]
    public async Task CreateLead_Valid_Returns201WithTheNewLeadsUrl()
    {
        var response = await CreateClient().PostAsJsonAsync("/api/leads", ValidLead(), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var lead = await response.Content.ReadFromJsonAsync<LeadResponse>(Ct);
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/leads/{lead!.Id}");
    }

    [Fact]
    public async Task CreateLead_AtAnExistingAddress_Returns409AddressAlreadyExists()
    {
        var client = CreateClient();
        (await client.PostAsJsonAsync("/api/leads", ValidLead(), Ct)).StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync("/api/leads", ValidLead(email: "john.roe@example.com"), Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "address.already_exists");
    }

    [Fact]
    public async Task ConvertLead_AlreadyConverted_Returns409LeadAlreadyConverted()
    {
        var client = CreateClient();
        await SeedLeadAsync(LeadStatus.Contacted);
        (await client.PostAsJsonAsync("/api/leads/convert", new ConvertLeadRequest { LeadId = 1 }, Ct))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync("/api/leads/convert", new ConvertLeadRequest { LeadId = 1 }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "lead.already_converted");
    }

    [Fact]
    public async Task ConvertLead_CustomerEmailTaken_Returns409CustomerEmailTaken()
    {
        var lead = await SeedLeadAsync(LeadStatus.Contacted);
        await SeedAsync(new Customer
        {
            FirstName = "Existing",
            LastName = "Customer",
            Email = lead.Email,
            PhoneNumber = "512-555-0101",
            ContactAddressId = 1
        });

        var response = await CreateClient().PostAsJsonAsync("/api/leads/convert",
            new ConvertLeadRequest { LeadId = 1 }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "customer.email_taken");
    }

    [Fact]
    public async Task CreateLead_InvalidFields_Returns400ValidationFailedWithTheFields()
    {
        var request = ValidLead();
        request.FirstName = "";

        var response = await CreateClient().PostAsJsonAsync("/api/leads", request, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").TryGetProperty("FirstName", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateLead_MalformedJson_Returns400ValidationFailed()
    {
        var response = await CreateClient().PostAsync("/api/leads",
            new StringContent("{\"firstName\":", Encoding.UTF8, "application/json"), Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task UpdateLeadStatus_UnknownStatus_Returns400ValidationFailed()
    {
        // Regression: an unknown status name reached Enum.Parse in the service and answered 500.
        await SeedLeadAsync(LeadStatus.New);

        var response = await CreateClient().PutAsJsonAsync("/api/leads/1/status",
            new UpdateLeadStatusRequest { Status = "Foo" }, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").TryGetProperty("Status", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UnknownUrl_Returns404WithAProblemBody()
    {
        var response = await CreateClient().GetAsync("/api/nope", Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }

    [Fact]
    public async Task UnhandledException_Returns500InternalErrorWithoutTheExceptionDetails()
    {
        var client = CreateClient(services => services.AddScoped<ILeadService, ThrowingLeadService>());

        var response = await client.GetAsync("/api/leads/1", Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.InternalServerError, "internal_error");
        body.GetRawText().Should().NotContain(ThrowingLeadService.Secret).And.NotContain("ThrowingLeadService");
    }

    private async Task<Lead> SeedLeadAsync(LeadStatus status)
    {
        await SeedAsync(AddressFactory.CreateFakeAddress(id: 1));
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1, status: status);
        await SeedAsync(lead);
        return lead;
    }

    /// <summary>A service with a bug: every call throws.</summary>
    private sealed class ThrowingLeadService : ILeadService
    {
        public const string Secret = "internal detail that must not leave the server";

        public Task<PagedResponse<LeadResponse>> GetPageAsync(PageRequest request) => throw new InvalidOperationException(Secret);
        public Task<Result<LeadResponse>> GetByIdAsync(int id) => throw new InvalidOperationException(Secret);
        public Task<Result<LeadResponse>> CreateAsync(CreateLeadRequest request) => throw new InvalidOperationException(Secret);
        public Task<Result> UpdateLeadStatusAsync(int id, UpdateLeadStatusRequest request) => throw new InvalidOperationException(Secret);
        public Task DeleteAsync(int id) => throw new InvalidOperationException(Secret);
        public Task<Result> ConvertLeadToCustomerAsync(ConvertLeadRequest request) => throw new InvalidOperationException(Secret);
    }
}
