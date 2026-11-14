using System.Net;
using System.Net.Http.Json;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Api;

// Every incoming timestamp must carry Z or an offset, and is stored as UTC.
public class TimestampTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("2026-11-02T10:00:00Z")]
    [InlineData("2026-11-02T11:00:00+01:00")]
    [InlineData("2026-11-02T04:00:00-06:00")]
    public async Task UpdateLeadStatus_TimestampWithAZone_IsStoredAsTheSameInstantInUtc(string contactedAt)
    {
        // Regression: an offset arrived as local time, and Npgsql refused it with a 500.
        await SeedLeadAsync();

        var response = await PutStatusAsync($"{{\"status\":\"Contacted\",\"contactedAt\":\"{contactedAt}\"}}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var lead = await Context.Leads.AsNoTracking().SingleAsync(l => l.Id == 1, Ct);
        lead.ContactedAt.Should().Be(new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc));
        lead.ContactedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task UpdateLeadStatus_TimestampWithoutAZone_Returns400ValidationFailed()
    {
        // Regression: a timestamp without a zone answered 500.
        await SeedLeadAsync();

        var response = await PutStatusAsync("{\"status\":\"Contacted\",\"contactedAt\":\"2026-11-02T10:00:00\"}");

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").GetRawText().Should().Contain("must include a time zone");
        var lead = await Context.Leads.AsNoTracking().SingleAsync(l => l.Id == 1, Ct);
        lead.Status.Should().Be(LeadStatus.New);
    }

    private async Task<HttpResponseMessage> PutStatusAsync(string json) =>
        await (await CreateClientAsAsync(UserRole.Admin)).PutAsync("/api/leads/1/status",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"), Ct);

    private async Task SeedLeadAsync()
    {
        await SeedAsync(AddressFactory.CreateFakeAddress(id: 1));
        await SeedAsync(LeadFactory.CreateFakeLead(id: 1, addressId: 1, status: LeadStatus.New));
    }
}
