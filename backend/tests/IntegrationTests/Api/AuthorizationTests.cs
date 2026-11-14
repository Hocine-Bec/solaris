using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestUtilities.Database;
using WebAPI.Security;

namespace IntegrationTests.Api;

// Who may call what (ADR 9): every endpoint needs a signed-in account unless it is open to anyone, the leads are the
// office's, and a session ends with its account (ADR 7).
public class AuthorizationTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The lead endpoints only the office may call.</summary>
    public static TheoryData<string, string> OfficeLeadEndpoints => new()
    {
        { "GET", "/api/leads" },
        { "GET", "/api/leads/1" },
        { "PUT", "/api/leads/1/status" },
        { "POST", "/api/leads/convert" },
    };

    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "PUT" or "POST" ? JsonContent.Create(new { }) : null
    };

    #region The leads

    [Theory]
    [MemberData(nameof(OfficeLeadEndpoints))]
    public async Task OfficeLeadEndpoint_SignedOut_Returns401(string method, string url)
    {
        var response = await CreateClient().SendAsync(Request(method, url), Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
    }

    [Theory]
    [MemberData(nameof(OfficeLeadEndpoints))]
    public async Task OfficeLeadEndpoint_Technician_Returns403(string method, string url)
    {
        var client = await CreateClientAsAsync(UserRole.Technician);

        var response = await client.SendAsync(Request(method, url), Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "forbidden");
    }

    [Fact]
    public async Task LeadList_Admin_Returns200()
    {
        var client = await CreateClientAsAsync(UserRole.Admin);

        var response = await client.GetAsync("/api/leads", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task QuoteForm_SignedOut_CreatesTheLead()
    {
        // The website's form: open to anyone
        var response = await CreateClient().PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region Sessions

    [Fact]
    public async Task ValidToken_AccountDeactivatedSince_Returns401SignedOut()
    {
        var client = await CreateClientAsAsync(UserRole.Admin);
        await Context.Users.Where(u => u.Email == TestAdmin.Email)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.IsActive, false), Ct);

        var response = await client.GetAsync("/api/leads", Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "auth.signed_out");
    }

    [Fact]
    public async Task ValidToken_AccountNoLongerExists_Returns401SignedOut()
    {
        var client = await CreateClientAsAsync(UserRole.Technician);
        await Context.Users.Where(u => u.Email == TestTechnician.Email).ExecuteDeleteAsync(Ct);

        var response = await client.GetAsync("/api/auth/me", Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "auth.signed_out");
    }

    #endregion

    #region Removed and unknown addresses

    [Theory]
    [InlineData("DELETE", "/api/leads/1", HttpStatusCode.MethodNotAllowed, "method_not_allowed")]
    [InlineData("GET", "/api/addresses", HttpStatusCode.NotFound, "not_found")]
    [InlineData("GET", "/api/addresses/1", HttpStatusCode.NotFound, "not_found")]
    [InlineData("GET", "/api/addresses/search?city=Austin", HttpStatusCode.NotFound, "not_found")]
    public async Task RemovedEndpoint_Admin_IsGone(string method, string url, HttpStatusCode status, string code)
    {
        // A lead is disqualified or marked lost, which keeps its history; addresses come with their lead
        var client = await CreateClientAsAsync(UserRole.Admin);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url), Ct);

        await ShouldBeProblemAsync(response, status, code);
    }

    [Fact]
    public async Task UnknownUrl_SignedOut_Returns401()
    {
        // Signing in is required before anything else, so a signed-out caller can't tell which URLs exist
        var response = await CreateClient().GetAsync("/api/nope", Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    [InlineData("/api/dev/email-fault")]
    public async Task DevelopmentOnlyAddress_SignedOut_IsOpen(string url)
    {
        // The API reference and the measurement harness exist in Development only, and are open there
        var response = await CreateClient(environment: "Development").GetAsync(url, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region The policies

    [Theory]
    [InlineData(Policies.Admin, "Admin", true)]
    [InlineData(Policies.Admin, "Technician", false)]
    [InlineData(Policies.Technician, "Technician", true)]
    [InlineData(Policies.Technician, "Admin", false)]
    [InlineData(Policies.Staff, "Admin", true)]
    [InlineData(Policies.Staff, "Technician", true)]
    public async Task Policy_ForRole_AllowsOnlyItsRoles(string policy, string role, bool allowed)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "1"), new Claim("role", role)], "Bearer",
            "sub", "role"));

        var result = await AuthorizationService().AuthorizeAsync(user, policy);

        result.Succeeded.Should().Be(allowed);
    }

    [Theory]
    [InlineData(Policies.Admin)]
    [InlineData(Policies.Technician)]
    [InlineData(Policies.Staff)]
    public async Task Policy_SignedOut_AllowsNobody(string policy)
    {
        var result = await AuthorizationService().AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), policy);

        result.Succeeded.Should().BeFalse();
    }

    private static IAuthorizationService AuthorizationService() =>
        new ServiceCollection().AddLogging().AddStaffPolicies().BuildServiceProvider()
            .GetRequiredService<IAuthorizationService>();

    #endregion
}
