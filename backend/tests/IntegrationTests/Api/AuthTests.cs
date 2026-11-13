using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.DTOs.Auth;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TestUtilities.Database;
using Identity = Microsoft.AspNetCore.Identity;

namespace IntegrationTests.Api;

// Staff sessions (FR-U1, FR-U3, ADR 7): signing in sets an HttpOnly cookie, /me reads the account, signing out clears it.
public class AuthTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static LoginRequest AsAdmin(string? password = null) =>
        new() { Email = TestAdmin.Email, Password = password ?? TestAdmin.Password };

    private static string SessionCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Should().ContainSingle(c => c.StartsWith("solaris_session=")).Subject;

    private static string TokenOf(string setCookie) => setCookie.Split(';')[0]["solaris_session=".Length..];

    /// <summary>Signs in as the test Admin and returns the session token from the cookie.</summary>
    private static async Task<string> SignInAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", AsAdmin(), Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return TokenOf(SessionCookie(response));
    }

    private static HttpRequestMessage WithSession(HttpMethod method, string url, string token, object? body = null,
        string? origin = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", $"solaris_session={token}");
        if (origin is not null) request.Headers.Add("Origin", origin);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task<JsonElement> WithoutTraceId(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(Ct);
        body!.Remove("traceId");
        return JsonSerializer.SerializeToElement(body);
    }

    #region Signing in

    [Fact]
    public async Task Login_RightCredentials_Returns200WithTheAccountAndSetsTheSessionCookie()
    {
        // Act
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", AsAdmin(), Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<CurrentUserResponse>(Ct))
            .Should().Be(new CurrentUserResponse(TestAdmin.Name, TestAdmin.Email, "Admin"));

        var cookie = SessionCookie(response).ToLowerInvariant();
        cookie.Should().Contain("httponly").And.Contain("secure").And.Contain("samesite=strict").And.Contain("path=/api");
        var expires = DateTimeOffset.Parse(cookie.Split("; ").Single(p => p.StartsWith("expires="))["expires=".Length..],
            CultureInfo.InvariantCulture);
        expires.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(8), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Login_EmailInAnotherCaseWithSpaces_SignsIn()
    {
        // Act
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "  ROSA.Castillo@Solaris.TEST ", Password = TestAdmin.Password }, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_RightCredentials_RecordsTheSignIn()
    {
        // Act
        await SignInAsync(CreateClient());

        // Assert
        var admin = await Context.Users.SingleAsync(Ct);
        admin.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401InvalidCredentialsWithoutACookie()
    {
        // Act
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", AsAdmin("Wrong#2026"), Ct);

        // Assert
        var body = await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "auth.invalid_credentials");
        body.GetProperty("detail").GetString().Should().Be("The email or password is incorrect.");
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Fact]
    public async Task Login_UnknownEmailOrDeactivatedAccount_AnswersExactlyLikeAWrongPassword()
    {
        // Arrange
        var client = CreateClient();
        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", AsAdmin("Wrong#2026"), Ct);

        // Act
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@solaris.test", Password = TestAdmin.Password }, Ct);
        await DeactivateAdminAsync();
        var deactivated = await client.PostAsJsonAsync("/api/auth/login", AsAdmin(), Ct);

        // Assert - one answer for every refusal, so a stranger learns nothing about which emails have an account
        var expected = await WithoutTraceId(wrongPassword);
        (await WithoutTraceId(unknownEmail)).GetRawText().Should().Be(expected.GetRawText());
        (await WithoutTraceId(deactivated)).GetRawText().Should().Be(expected.GetRawText());
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        deactivated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_MissingFields_Returns400ValidationFailed()
    {
        // Act
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(), Ct);

        // Assert
        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().BeEquivalentTo("Email", "Password");
    }

    [Fact]
    public async Task Login_HashMadeWithFewerIterations_SignsInAndReplacesTheHash()
    {
        // Arrange - a technician whose hash was made with the framework's default, 100,000 iterations
        var client = CreateClient();
        var technician = UserRepoTests.NewUser("jamal.ortiz@solaris.test", UserRole.Technician);
        technician.PasswordHash = new Identity.PasswordHasher<User>().HashPassword(technician, "Panels#2026");
        await SeedAsync(technician);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "jamal.ortiz@solaris.test", Password = "Panels#2026" }, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Context.Users.SingleAsync(u => u.Email == "jamal.ortiz@solaris.test", Ct);
        IterationsOf(stored.PasswordHash).Should().Be(220_000);
    }

    #endregion

    #region Who is signed in

    [Fact]
    public async Task Me_WithTheSessionCookie_ReturnsTheAccount()
    {
        // Arrange
        var client = CreateClient();
        var token = await SignInAsync(client);

        // Act
        var response = await client.SendAsync(WithSession(HttpMethod.Get, "/api/auth/me", token), Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<CurrentUserResponse>(Ct))
            .Should().Be(new CurrentUserResponse(TestAdmin.Name, TestAdmin.Email, "Admin"));
    }

    [Fact]
    public async Task Me_WithTheTokenInTheAuthorizationHeader_ReturnsTheAccount()
    {
        // Arrange
        var client = CreateClient();
        var token = await SignInAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", token);

        // Act
        var response = await client.SendAsync(request, Ct);

        // Assert - the API reference, tests and scripts use the header
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_WithoutASession_Returns401()
    {
        // Act
        var response = await CreateClient().GetAsync("/api/auth/me", Ct);

        // Assert
        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
    }

    [Fact]
    public async Task Me_AccountDeactivatedSinceSigningIn_Returns401SignedOut()
    {
        // Arrange
        var client = CreateClient();
        var token = await SignInAsync(client);
        await DeactivateAdminAsync();

        // Act
        var response = await client.SendAsync(WithSession(HttpMethod.Get, "/api/auth/me", token), Ct);

        // Assert - the account is read on each request, so a deactivation stops its sessions at once
        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "auth.signed_out");
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("wrong key")]
    [InlineData("tampered")]
    public async Task Me_WithABadToken_Returns401(string what)
    {
        // Arrange
        var client = CreateClient();
        var token = what switch
        {
            "expired" => Token("test-secret-test-secret-test-secret-test-secret", DateTime.UtcNow.AddHours(-9),
                DateTime.UtcNow.AddHours(-1)),
            "wrong key" => Token("another-secret-another-secret-another-secret", DateTime.UtcNow, DateTime.UtcNow.AddHours(8)),
            _ => Tamper(await SignInAsync(client))
        };

        // Act
        var response = await client.SendAsync(WithSession(HttpMethod.Get, "/api/auth/me", token), Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Signing out

    [Fact]
    public async Task Logout_ClearsTheSessionCookie()
    {
        // Arrange
        var client = CreateClient();
        var token = await SignInAsync(client);

        // Act
        var response = await client.SendAsync(WithSession(HttpMethod.Post, "/api/auth/logout", token), Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var cookie = SessionCookie(response).ToLowerInvariant();
        cookie.Should().StartWith("solaris_session=;").And.Contain("expires=thu, 01 jan 1970").And.Contain("path=/api");
    }

    [Fact]
    public async Task Logout_WithoutASession_StillReturns204()
    {
        // Act
        var response = await CreateClient().PostAsync("/api/auth/logout", null, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region Requests from another site (ADR 7)

    [Fact]
    public async Task Login_FromAPageOnAnotherSite_Returns403CrossSiteRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(AsAdmin()) };
        request.Headers.Add("Origin", "https://elsewhere.example");

        // Act
        var response = await CreateClient().SendAsync(request, Ct);

        // Assert
        var body = await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "auth.cross_site_request");
        body.GetProperty("detail").GetString().Should().Be("This request must come from the Solaris staff app.");
    }

    [Fact]
    public async Task Login_FromAPageOnTheApisOwnSite_SignsIn()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(AsAdmin()) };
        request.Headers.Add("Origin", "http://localhost");

        // Act
        var response = await CreateClient().SendAsync(request, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeWithTheSessionCookie_FromAnotherSite_Returns403()
    {
        // Arrange
        var client = CreateClient();
        var token = await SignInAsync(client);

        // Act
        var response = await client.SendAsync(WithSession(HttpMethod.Post, "/api/leads", token,
            ErrorResponseTests.ValidLead(), origin: "https://elsewhere.example"), Ct);

        // Assert
        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "auth.cross_site_request");
    }

    [Fact]
    public async Task ReadWithTheSessionCookie_FromAnotherSite_IsNotBlocked()
    {
        // Arrange
        var client = CreateClient();
        var token = await SignInAsync(client);

        // Act
        var response = await client.SendAsync(WithSession(HttpMethod.Get, "/api/auth/me", token,
            origin: "https://elsewhere.example"), Ct);

        // Assert - reading changes nothing; SameSite=Strict keeps the cookie off other sites' requests anyway
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task QuoteRequest_FromTheMarketingSiteWithoutACookie_IsNotBlocked()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads") { Content = JsonContent.Create(ErrorResponseTests.ValidLead()) };
        request.Headers.Add("Origin", "https://www.solaris.example");

        // Act
        var response = await CreateClient().SendAsync(request, Ct);

        // Assert - the public form carries no session, so the check doesn't apply
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region The first Admin (FR-U3)

    [Fact]
    public async Task Startup_NoAdmin_CreatesTheFirstAdminFromTheSettingsWithAHashedPassword()
    {
        // Act
        CreateClient();

        // Assert
        var admin = await Context.Users.SingleAsync(Ct);
        admin.Should().BeEquivalentTo(new
        {
            Name = TestAdmin.Name,
            Email = TestAdmin.Email,
            NormalizedEmail = TestAdmin.Email.ToUpperInvariant(),
            Role = UserRole.Admin,
            IsActive = true,
            LastLoginAt = (DateTime?)null
        });
        admin.PasswordHash.Should().NotContain(TestAdmin.Password);
        IterationsOf(admin.PasswordHash).Should().Be(220_000);
    }

    [Fact]
    public async Task Startup_AnActiveAdminExists_CreatesNoOther()
    {
        // Arrange
        await SeedAsync(UserRepoTests.NewUser("first.admin@solaris.test"));

        // Act
        CreateClient();

        // Assert
        (await Context.Users.Select(u => u.Email).ToListAsync(Ct)).Should().Equal("first.admin@solaris.test");
    }

    [Fact]
    public void Startup_NoAdminAndNoAdminSettings_StopsNamingTheSettings()
    {
        // Act
        var start = () => CreateClient(settings: new Dictionary<string, string>
        {
            ["Admin:Name"] = "", ["Admin:Email"] = "", ["Admin:Password"] = ""
        });

        // Assert - without an Admin, nobody could sign in to the staff app
        start.Should().Throw<InvalidOperationException>().WithMessage("*No active Admin account exists*Admin__Email*");
    }

    #endregion

    private async Task DeactivateAdminAsync() =>
        await Context.Users.Where(u => u.Email == TestAdmin.Email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false), Ct);

    /// <summary>The iteration count stored in an ASP.NET Core Identity V3 hash (format byte, PRF, iterations, …).</summary>
    private static int IterationsOf(string hash) =>
        (int)BinaryPrimitives.ReadUInt32BigEndian(Convert.FromBase64String(hash).AsSpan(5, 4));

    private static string Token(string secret, DateTime notBefore, DateTime expires) => new JsonWebTokenHandler().CreateToken(
        new SecurityTokenDescriptor
        {
            Issuer = "solaris-api",
            Audience = "solaris-staff",
            NotBefore = notBefore,
            IssuedAt = notBefore,
            Expires = expires,
            Claims = new Dictionary<string, object> { ["sub"] = "1", ["role"] = "Admin" },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256)
        });

    // The same token claiming another role: its signature no longer matches its payload.
    private static string Tamper(string token)
    {
        var parts = token.Split('.');
        var payload = Base64UrlEncoder.Decode(parts[1]);
        payload.Should().Contain("\"role\":\"Admin\"");
        parts[1] = Base64UrlEncoder.Encode(payload.Replace("\"role\":\"Admin\"", "\"role\":\"Technician\""));
        return string.Join('.', parts);
    }
}
