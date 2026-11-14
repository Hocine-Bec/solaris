using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestUtilities.Database;
using WebAPI.Security;

namespace IntegrationTests.Api;

// How often one address may use the quote form and check a password (BR-S1), and which address counts: the
// connection's, or behind a proxy the settings name, the address it forwarded. The API in memory has no connection
// address of its own, so these tests give each request one (X-Test-Peer, read before the API's own middleware).
public class RateLimitTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The quote form sent with a lead of its own (n), from this connection address, forwarding this header.</summary>
    private async Task<HttpResponseMessage> QuoteAsync(HttpClient client, int n, string? peer = null, string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(ErrorResponseTests.ValidLead($"visitor.{n}@example.com", $"{100 + n} Congress Ave"))
        };
        if (peer is not null) request.Headers.Add(TestPeerAddress.Header, peer);
        if (forwardedFor is not null) request.Headers.Add("X-Forwarded-For", forwardedFor);
        return await client.SendAsync(request, Ct);
    }

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = TestAdmin.Email, Password = password }, Ct);

    private static async Task<List<HttpStatusCode>> StatusesAsync(IEnumerable<Func<Task<HttpResponseMessage>>> requests)
    {
        var statuses = new List<HttpStatusCode>();
        foreach (var send in requests)
            statuses.Add((await send()).StatusCode);
        return statuses;
    }

    #region The quote form (BR-S1)

    [Fact]
    public async Task BR_S1_SixthQuoteFromOneAddressInAMinute_Returns429AndSavesNothing()
    {
        // Arrange - five quotes from one address, each a lead of its own
        var client = CreateClient();
        for (var n = 1; n <= 5; n++)
            (await QuoteAsync(client, n)).StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await QuoteAsync(client, 6);

        // Assert - when to try again: the window's length, by which time a new one has started
        var body = await ShouldBeProblemAsync(response, HttpStatusCode.TooManyRequests, "too_many_requests");
        body.GetProperty("detail").GetString().Should().Be("Too many requests from this address. Wait a minute, then try again.");
        response.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(60));
        (await Context.Leads.CountAsync(Ct)).Should().Be(5);
        Notifier.Notified.Should().HaveCount(5);
    }

    [Fact]
    public async Task BR_S1_QuotesRefusedByValidation_CountToo()
    {
        // Every request costs the server work, whatever its answer
        var client = CreateClient();
        var empty = () => client.PostAsJsonAsync("/api/leads", new { }, Ct);

        var statuses = await StatusesAsync(Enumerable.Repeat(empty, 6));

        statuses.Should().Equal(Enumerable.Repeat(HttpStatusCode.BadRequest, 5).Append(HttpStatusCode.TooManyRequests));
    }

    [Fact]
    public async Task BR_S1_AnotherAddress_HasItsOwnFive()
    {
        // Arrange - one address has used its five
        var client = CreateClient(WithTestPeers);
        for (var n = 1; n <= 5; n++)
            await QuoteAsync(client, n, peer: "203.0.113.10");

        // Act
        var other = await QuoteAsync(client, 6, peer: "203.0.113.11");
        var same = await QuoteAsync(client, 7, peer: "203.0.113.10");

        // Assert
        other.StatusCode.Should().Be(HttpStatusCode.Created);
        same.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task BR_S1_QuoteFormLimitReached_TheRestOfTheApiStillAnswers()
    {
        // Arrange
        var client = CreateClient();
        for (var n = 1; n <= 6; n++)
            await QuoteAsync(client, n);

        // Act - the office's list, and signing in, from the same address
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var leads = await admin.GetAsync("/api/leads", Ct);
        var signIn = await SignInAsync(client, TestAdmin.Password);

        // Assert
        leads.StatusCode.Should().Be(HttpStatusCode.OK);
        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Checking a password

    [Fact]
    public async Task PasswordChecks_SignInAndChangingYourPasswordShareTenAMinute()
    {
        // Arrange - nine wrong passwords at sign-in, then a wrong current password in a session: ten checks
        var client = CreateClient();
        var admin = await CreateClientAsAsync(UserRole.Admin);
        for (var i = 0; i < 9; i++)
            (await SignInAsync(client, "Wrong#2026")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await admin.PutAsJsonAsync("/api/auth/password",
                new ChangePasswordRequest { CurrentPassword = "Wrong#2026", NewPassword = "Solaris#2027" }, Ct))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Act - the eleventh, with the right password, and another change in the session
        var signIn = await SignInAsync(client, TestAdmin.Password);
        var change = await admin.PutAsJsonAsync("/api/auth/password",
            new ChangePasswordRequest { CurrentPassword = TestAdmin.Password, NewPassword = "Solaris#2027" }, Ct);

        // Assert - the session itself goes on: a 429 isn't a 401
        await ShouldBeProblemAsync(signIn, HttpStatusCode.TooManyRequests, "too_many_requests");
        await ShouldBeProblemAsync(change, HttpStatusCode.TooManyRequests, "too_many_requests");
        (await admin.GetAsync("/api/auth/me", Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PasswordChecks_RefusedBeforeTheLimit_DoNotCount()
    {
        // Arrange - twelve password changes without a session: authorization refuses them first
        var client = CreateClient();
        var body = new ChangePasswordRequest { CurrentPassword = TestAdmin.Password, NewPassword = "Solaris#2027" };
        for (var i = 0; i < 12; i++)
            (await client.PutAsJsonAsync("/api/auth/password", body, Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act
        var signIn = await SignInAsync(client, TestAdmin.Password);

        // Assert
        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Which address counts

    [Fact]
    public async Task NoProxyListed_ForwardedForIsIgnored_SoItCanNotDodgeTheLimit()
    {
        // Arrange - a client claiming a new address in each request
        var client = CreateClient(WithTestPeers);

        // Act
        var statuses = await StatusesAsync(Enumerable.Range(1, 6)
            .Select(n => (Func<Task<HttpResponseMessage>>)(() => QuoteAsync(client, n, "203.0.113.10", $"198.51.100.{n}"))));

        // Assert
        statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ListedProxy_TheAddressItForwardedCounts()
    {
        // Arrange - the API behind a proxy in 10.0.0.0/8, forwarding six visitors, then one visitor six times
        var client = CreateClient(WithTestPeers, settings: ProxyIn("10.0.0.0/8"));

        // Act
        var visitors = await StatusesAsync(Enumerable.Range(1, 6)
            .Select(n => (Func<Task<HttpResponseMessage>>)(() => QuoteAsync(client, n, "10.1.2.3", $"198.51.100.{n}"))));
        var oneVisitor = await StatusesAsync(Enumerable.Range(7, 6)
            .Select(n => (Func<Task<HttpResponseMessage>>)(() => QuoteAsync(client, n, "10.1.2.3", "198.51.100.200"))));

        // Assert
        visitors.Should().AllBeEquivalentTo(HttpStatusCode.Created);
        oneVisitor.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ListedProxy_OnlyTheAddressItAddedCounts_NotOnesTheClientWrote()
    {
        // Arrange - the client writes a new address first in the header each time; the proxy adds the real one last
        var client = CreateClient(WithTestPeers, settings: ProxyIn("10.0.0.0/8"));

        // Act
        var statuses = await StatusesAsync(Enumerable.Range(1, 6)
            .Select(n => (Func<Task<HttpResponseMessage>>)(() =>
                QuoteAsync(client, n, "10.1.2.3", $"192.0.2.{n}, 198.51.100.7"))));

        // Assert
        statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ProxyListed_AConnectionFromElsewhere_IsNotBelieved()
    {
        // Arrange - a proxy is listed, but this connection doesn't come from it
        var client = CreateClient(WithTestPeers, settings: ProxyIn("10.0.0.0/8"));

        // Act
        var statuses = await StatusesAsync(Enumerable.Range(1, 6)
            .Select(n => (Func<Task<HttpResponseMessage>>)(() => QuoteAsync(client, n, "203.0.113.10", $"198.51.100.{n}"))));

        // Assert
        statuses.Last().Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task IPv4ClientOnADualStackConnection_CountsAsItsIPv4Address()
    {
        var client = CreateClient(WithTestPeers);
        for (var n = 1; n <= 5; n++)
            await QuoteAsync(client, n, peer: "203.0.113.10");

        var response = await QuoteAsync(client, 6, peer: "::ffff:203.0.113.10");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static Dictionary<string, string> ProxyIn(string network) =>
        new() { ["ForwardedHeaders:KnownNetworks:0"] = network };

    #endregion

    #region The settings

    [Fact]
    public async Task SwitchedOffInDevelopment_NoLimit()
    {
        // The measurement runs send many leads from one machine, in Development
        var client = CreateClient(environment: "Development", settings: new Dictionary<string, string>
        {
            ["RateLimiting:Enabled"] = "false"
        });

        var statuses = await StatusesAsync(Enumerable.Range(1, 6)
            .Select(n => (Func<Task<HttpResponseMessage>>)(() => QuoteAsync(client, n))));

        statuses.Should().AllBeEquivalentTo(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void SwitchedOffOutsideDevelopment_TheApiDoesNotStart(string environment)
    {
        var start = () => CreateClient(environment: environment, settings: new Dictionary<string, string>
        {
            ["RateLimiting:Enabled"] = "false"
        });

        start.Should().Throw<OptionsValidationException>().WithMessage("*RateLimiting:Enabled can be false only in Development*");
    }

    [Theory]
    [InlineData("10.0.0.0")]
    [InlineData("10.0.0.0/33")]
    [InlineData("proxy.example")]
    public void ProxyNetworkNotANetwork_TheApiDoesNotStart(string network)
    {
        var start = () => CreateClient(settings: ProxyIn(network));

        start.Should().Throw<OptionsValidationException>().WithMessage("*ForwardedHeaders:KnownNetworks*");
    }

    [Fact]
    public void LimitedEndpoints_AreTheQuoteFormSignInAndChangingYourPassword()
    {
        CreateClient();

        var limited = ApiServices.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints
            .Select(e => (Endpoint: e.DisplayName ?? "", Policy: e.Metadata
                .GetMetadata<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()?.PolicyName))
            .Where(e => e.Policy is not null)
            .Select(e => $"{e.Policy}: {e.Endpoint.Split(" (")[0]}");

        limited.Should().BeEquivalentTo(
            $"{RateLimits.QuoteForm}: WebAPI.Controllers.LeadsController.Create",
            $"{RateLimits.PasswordChecks}: WebAPI.Controllers.AuthController.Login",
            $"{RateLimits.PasswordChecks}: WebAPI.Controllers.AuthController.ChangePassword");
    }

    #endregion
}
