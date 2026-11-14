using System.Net;
using System.Net.Http.Json;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// What other sites and machines can reach (BR-S2): CORS lets the listed sites' pages send the quote form, and nothing
// else; the background jobs' dashboard exists in Development only, for requests from the same machine.
public class CrossSiteTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string Site = "https://www.solaris.test";

    private static Dictionary<string, string> Allowing(string origin) => new() { ["Cors:AllowedOrigins:0"] = origin };

    /// <summary>The browser's question before a JSON POST from another site's page.</summary>
    private static HttpRequestMessage Preflight(string url, string origin, string method = "POST")
    {
        var request = new HttpRequestMessage(HttpMethod.Options, url);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    private static HttpRequestMessage QuoteFrom(string origin, int n = 1)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(ErrorResponseTests.ValidLead($"visitor.{n}@example.com", $"{100 + n} Congress Ave"))
        };
        request.Headers.Add("Origin", origin);
        return request;
    }

    private static string? AllowedOrigin(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;

    #region CORS: the quote form

    [Fact]
    public async Task QuoteForm_PreflightFromAListedSite_AllowsAJsonPostWithoutCookies()
    {
        var client = CreateClient(settings: Allowing(Site));

        var response = await client.SendAsync(Preflight("/api/leads", Site), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        AllowedOrigin(response).Should().Be(Site);
        response.Headers.GetValues("Access-Control-Allow-Methods").Should().Equal("POST");
        response.Headers.GetValues("Access-Control-Allow-Headers").Single().Should().BeEquivalentTo("content-type");
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    [Fact]
    public async Task QuoteForm_SentFromAListedSite_IsCreatedAndReadable()
    {
        var client = CreateClient(settings: Allowing(Site));

        var response = await client.SendAsync(QuoteFrom(Site), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AllowedOrigin(response).Should().Be(Site);
    }

    [Fact]
    public async Task QuoteForm_RefusedByTheRateLimit_TheListedSiteCanStillReadWhy()
    {
        // Without the header, the browser would hide the 429, and the site would say the server didn't answer
        var client = CreateClient(settings: Allowing(Site));
        for (var n = 1; n <= 5; n++)
            await client.SendAsync(QuoteFrom(Site, n), Ct);

        var response = await client.SendAsync(QuoteFrom(Site, 6), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        AllowedOrigin(response).Should().Be(Site);
    }

    [Theory]
    [InlineData("https://elsewhere.example")]
    [InlineData("http://www.solaris.test")]          // the same host over HTTP is another origin
    [InlineData("https://www.solaris.test:8443")]    // and so is another port
    public async Task QuoteForm_FromASiteNotListed_GetsNoPermission(string origin)
    {
        var client = CreateClient(settings: Allowing(Site));

        var preflight = await client.SendAsync(Preflight("/api/leads", origin), Ct);

        AllowedOrigin(preflight).Should().BeNull();
    }

    [Fact]
    public async Task QuoteForm_NoSiteListed_NoOtherSiteMayCallIt()
    {
        var client = CreateClient();

        var preflight = await client.SendAsync(Preflight("/api/leads", Site), Ct);

        AllowedOrigin(preflight).Should().BeNull();
    }

    [Fact]
    public async Task QuoteForm_InDevelopment_TheLocalSiteMayCallIt()
    {
        // appsettings.Development.json lists the marketing site's development server
        var client = CreateClient(environment: "Development");

        var preflight = await client.SendAsync(Preflight("/api/leads", "http://localhost:5173"), Ct);

        AllowedOrigin(preflight).Should().Be("http://localhost:5173");
    }

    #endregion

    #region CORS: everything else

    [Theory]
    [InlineData("/api/auth/login", "POST")]
    [InlineData("/api/auth/me", "GET")]
    [InlineData("/api/leads", "GET")]
    [InlineData("/api/users", "POST")]
    public async Task OtherEndpoint_EvenFromAListedSite_GetsNoPermission(string url, string method)
    {
        var client = CreateClient(settings: Allowing(Site));

        var preflight = await client.SendAsync(Preflight(url, Site, method), Ct);

        AllowedOrigin(preflight).Should().BeNull();
    }

    [Fact]
    public async Task OtherEndpoint_ReadFromAListedSite_AnswersWithoutPermission()
    {
        // The Admin's list, even with a valid session, isn't readable by another site's page
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/leads");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await admin.SendAsync(request, Ct);

        AllowedOrigin(response).Should().BeNull();
    }

    [Theory]
    [InlineData("https://www.solaris.test/")]
    [InlineData("https://www.solaris.test/quote")]
    [InlineData("*")]
    [InlineData("www.solaris.test")]
    [InlineData("ftp://www.solaris.test")]
    public void SiteNotAnOrigin_TheApiDoesNotStart(string origin)
    {
        var start = () => CreateClient(settings: Allowing(origin));

        start.Should().Throw<OptionsValidationException>().WithMessage("*Cors:AllowedOrigins*");
    }

    #endregion

    #region The background jobs' dashboard

    [Fact]
    public async Task Dashboard_InDevelopmentFromThisMachine_Opens()
    {
        var client = CreateClient(WithTestPeers, environment: "Development");
        var request = new HttpRequestMessage(HttpMethod.Get, "/hangfire");
        request.Headers.Add(TestPeerAddress.Header, "127.0.0.1");

        var response = await client.SendAsync(request, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Dashboard_InDevelopmentFromAnotherMachine_IsRefused()
    {
        var client = CreateClient(WithTestPeers, environment: "Development");
        var request = new HttpRequestMessage(HttpMethod.Get, "/hangfire");
        request.Headers.Add(TestPeerAddress.Header, "203.0.113.10");

        var response = await client.SendAsync(request, Ct);

        new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden }.Should().Contain(response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_OutsideDevelopment_DoesNotExist()
    {
        // Signed out, closed like any address (ADR 9); signed in as the Admin, from this machine, not found
        var signedOut = await CreateClient(WithTestPeers).GetAsync("/hangfire", Ct);
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var request = new HttpRequestMessage(HttpMethod.Get, "/hangfire");
        request.Headers.Add(TestPeerAddress.Header, "127.0.0.1");
        var asAdmin = await admin.SendAsync(request, Ct);

        await ShouldBeProblemAsync(signedOut, HttpStatusCode.Unauthorized, "unauthorized");
        await ShouldBeProblemAsync(asAdmin, HttpStatusCode.NotFound, "not_found");
    }

    #endregion
}
