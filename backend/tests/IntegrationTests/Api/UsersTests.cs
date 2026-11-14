using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Auth;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// Staff accounts, managed by the Admin (FR-U2, FR-U4): the list, one account, a new technician, editing, a new
// password, deactivating and reactivating; and each person changing their own password. A new password or a
// deactivation ends the account's sessions at their next request (ADR 7).
public class UsersTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string LenaEmail = "lena.park@solaris.test";
    private const string LenaPassword = "Panels#2026";

    private static CreateUserRequest Lena(string? email = null, string? password = null) =>
        new() { Name = "Lena Park", Email = email ?? LenaEmail, Password = password ?? LenaPassword };

    /// <summary>Creates a technician through the API, as the Admin's screen does, and returns their ID.</summary>
    private static async Task<int> CreateAsync(HttpClient admin, CreateUserRequest request)
    {
        var response = await admin.PostAsJsonAsync("/api/users", request, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<UserResponse>(Ct))!.Id;
    }

    private async Task<HttpResponseMessage> SignInAsync(string email, string password) =>
        await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password }, Ct);

    /// <summary>The session token a successful sign-in puts in its cookie.</summary>
    private static string SessionOf(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("solaris_session="))
            .Split(';')[0]["solaris_session=".Length..];

    /// <summary>
    /// A session of this account that started a minute ago, before whatever the test then changes: a password set in
    /// the same second as a session started doesn't end it (sessions are compared in whole seconds).
    /// </summary>
    private static string EarlierSession(int userId, UserRole role)
    {
        var started = DateTime.UtcNow.AddMinutes(-1);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "solaris-api",
            Audience = "solaris-staff",
            IssuedAt = started,
            NotBefore = started,
            Expires = started.AddHours(8),
            Claims = new Dictionary<string, object> { ["sub"] = userId.ToString(), ["role"] = role.ToString() },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-test-secret-test-secret-test-secret")),
                SecurityAlgorithms.HmacSha256)
        });
    }

    /// <summary>A request with this session's token, as the staff app's cookie or a script's header carries it.</summary>
    private async Task<HttpResponseMessage> AsSessionAsync(string token, HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await CreateClient().SendAsync(request, Ct);
    }

    private Task<User> StoredAsync(int id) => Context.Users.AsNoTracking().SingleAsync(u => u.Id == id, Ct);

    #region The list

    [Fact]
    public async Task GetPage_Admin_ReturnsEveryAccountActiveFirstThenByName()
    {
        // Arrange - two technicians, and one deactivated
        var admin = await CreateClientAsAsync(UserRole.Admin);
        await CreateAsync(admin, Lena());
        await CreateAsync(admin, new CreateUserRequest { Name = "Ava Stone", Email = "ava.stone@solaris.test", Password = LenaPassword });
        var diego = UserRepoTests.NewUser("diego.ruiz@solaris.test", UserRole.Technician, isActive: false);
        diego.Name = "Diego Ruiz";
        await SeedAsync(diego);

        // Act
        var response = await admin.GetAsync("/api/users", Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        page.GetProperty("totalCount").GetInt32().Should().Be(4);
        var items = page.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("name").GetString())
            .Should().Equal("Ava Stone", "Lena Park", TestAdmin.Name, "Diego Ruiz");
        // Each account's fields, and never its password
        items[0].EnumerateObject().Select(p => p.Name)
            .Should().Equal("id", "name", "email", "role", "isActive", "createdAt", "lastLoginAt");
        items[3].GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    #endregion

    #region One account

    [Fact]
    public async Task GetById_NewTechnician_ReturnsTheAccountNeverSignedIn()
    {
        // Arrange
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());

        // Act
        var user = await admin.GetFromJsonAsync<UserResponse>($"/api/users/{id}", Ct);

        // Assert
        user!.Should().BeEquivalentTo(new { Id = id, Name = "Lena Park", Email = LenaEmail, Role = "Technician", IsActive = true });
        user.LastLoginAt.Should().BeNull();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);

        var response = await admin.GetAsync("/api/users/999", Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "user.not_found");
        body.GetProperty("detail").GetString().Should().Be("User with ID 999 not found.");
    }

    #endregion

    #region A new technician (FR-U2)

    [Fact]
    public async Task Create_ValidTechnician_Returns201WithTheAccount_AndTheyCanSignIn()
    {
        // Arrange - the name and email as typed, with spaces around them
        var admin = await CreateClientAsAsync(UserRole.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/api/users",
            Lena(email: "  Lena.Park@Solaris.test "), Ct);

        // Assert - stored trimmed, a technician, active; the password never comes back
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<UserResponse>(Ct);
        created!.Should().BeEquivalentTo(new { Name = "Lena Park", Email = "Lena.Park@Solaris.test", Role = "Technician", IsActive = true });
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/users/{created.Id}");
        (await response.Content.ReadAsStringAsync(Ct)).Should().NotContainEquivalentOf("password");

        var signIn = await SignInAsync(LenaEmail, LenaPassword);
        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
        (await signIn.Content.ReadFromJsonAsync<CurrentUserResponse>(Ct))!.Role.Should().Be("Technician");
    }

    [Theory]
    [InlineData("rosa.castillo@solaris.test")]
    [InlineData("ROSA.Castillo@SOLARIS.test")]
    [InlineData("  rosa.castillo@solaris.test ")]
    public async Task Create_EmailAnAccountAlreadyHasWhateverItsCaseOrSpaces_Returns409(string email)
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);

        var response = await admin.PostAsJsonAsync("/api/users", Lena(email: email), Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "user.email_taken");
    }

    [Fact]
    public async Task Create_EmailOfADeactivatedAccount_Returns409()
    {
        // Arrange - a deactivated account keeps its email: reactivating it is the way back
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        (await admin.PostAsync($"/api/users/{id}/deactivate", null, Ct)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var response = await admin.PostAsJsonAsync("/api/users", Lena(), Ct);

        // Assert
        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "user.email_taken");
    }

    [Fact]
    public async Task Create_FieldsBreakingTheRules_Returns400NamingEachField()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);

        var response = await admin.PostAsJsonAsync("/api/users",
            new CreateUserRequest { Name = "   ", Email = "lena.park", Password = "panels2026" }, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        var errors = body.GetProperty("errors");
        errors.EnumerateObject().Select(e => e.Name).Should().BeEquivalentTo("Name", "Email", "Password");
        errors.GetProperty("Password").EnumerateArray().Select(m => m.GetString()).Should().BeEquivalentTo(
            "Password must contain an uppercase letter", "Password must contain a special character, such as ! or #");
    }

    [Fact]
    public async Task Create_SameEmailTwiceAtTheSameMoment_CreatesOneAccount()
    {
        // Arrange - two clients, released together: the unique index decides, not a check before the insert
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var token = admin.DefaultRequestHeaders.Authorization!.Parameter!;
        var start = new TaskCompletionSource();
        var requests = Enumerable.Range(0, 2).Select(async _ =>
        {
            await start.Task;
            return await AsSessionAsync(token, HttpMethod.Post, "/api/users", Lena());
        }).ToList();

        // Act
        start.SetResult();
        var responses = await Task.WhenAll(requests);

        // Assert
        responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
        (await Context.Users.CountAsync(u => u.NormalizedEmail == User.NormalizeEmail(LenaEmail), Ct)).Should().Be(1);
    }

    #endregion

    #region Editing

    [Fact]
    public async Task Update_NameAndEmail_Saved_AndThePasswordAndSessionsStay()
    {
        // Arrange - Lena signed in a minute ago
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        var session = EarlierSession(id, UserRole.Technician);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/users/{id}",
            new UpdateUserRequest { Name = " Lena Park-Ito ", Email = " lena.ito@solaris.test " }, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stored = await StoredAsync(id);
        (stored.Name, stored.Email, stored.NormalizedEmail).Should().Be(("Lena Park-Ito", "lena.ito@solaris.test", "LENA.ITO@SOLARIS.TEST"));
        (await AsSessionAsync(session, HttpMethod.Get, "/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SignInAsync("lena.ito@solaris.test", LenaPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_OnlyTheCaseOfTheirOwnEmail_Saved()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());

        var response = await admin.PutAsJsonAsync($"/api/users/{id}",
            new UpdateUserRequest { Name = "Lena Park", Email = "Lena.Park@solaris.test" }, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredAsync(id)).Email.Should().Be("Lena.Park@solaris.test");
    }

    [Fact]
    public async Task Update_EmailOfAnotherAccount_Returns409AndChangesNothing()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());

        var response = await admin.PutAsJsonAsync($"/api/users/{id}",
            new UpdateUserRequest { Name = "Lena Park", Email = TestAdmin.Email.ToUpperInvariant() }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "user.email_taken");
        (await StoredAsync(id)).Email.Should().Be(LenaEmail);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);

        var response = await admin.PutAsJsonAsync("/api/users/999",
            new UpdateUserRequest { Name = "Lena Park", Email = LenaEmail }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "user.not_found");
    }

    #endregion

    #region A new password from the office

    [Fact]
    public async Task SetPassword_Technician_TheNewOneSignsIn_TheOldOneDoesNot_AndTheirSessionsEnd()
    {
        // Arrange
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        var session = EarlierSession(id, UserRole.Technician);

        // Act
        var response = await admin.PutAsJsonAsync($"/api/users/{id}/password",
            new SetPasswordRequest { Password = "Inverter#77" }, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ShouldBeProblemAsync(await AsSessionAsync(session, HttpMethod.Get, "/api/auth/me"),
            HttpStatusCode.Unauthorized, "auth.signed_out");
        await ShouldBeProblemAsync(await SignInAsync(LenaEmail, LenaPassword),
            HttpStatusCode.Unauthorized, "auth.invalid_credentials");
        var signIn = await SignInAsync(LenaEmail, "Inverter#77");
        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AsSessionAsync(SessionOf(signIn), HttpMethod.Get, "/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetPassword_OwnAccount_Returns409AndChangesNothing()
    {
        // Your own password is changed with your current one, so an open session can't replace it alone
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var adminId = await IdOfAsync(TestAdmin.Email);

        var response = await admin.PutAsJsonAsync($"/api/users/{adminId}/password",
            new SetPasswordRequest { Password = "Inverter#77" }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "user.cannot_set_own_password");
        (await SignInAsync(TestAdmin.Email, TestAdmin.Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetPassword_BreakingTheRule_Returns400()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());

        var response = await admin.PutAsJsonAsync($"/api/users/{id}/password",
            new SetPasswordRequest { Password = "Short#1" }, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").GetProperty("Password")[0].GetString()
            .Should().Be("Password must be at least 8 characters");
    }

    [Fact]
    public async Task SetPassword_UnknownId_Returns404()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);

        var response = await admin.PutAsJsonAsync("/api/users/999/password",
            new SetPasswordRequest { Password = "Inverter#77" }, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "user.not_found");
    }

    #endregion

    #region Deactivating and reactivating (FR-U2)

    [Fact]
    public async Task Deactivate_Technician_TheirSessionEndsAndSignInIsRefused_UntilReactivated()
    {
        // Arrange
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        var session = SessionOf(await SignInAsync(LenaEmail, LenaPassword));

        // Act
        var deactivated = await admin.PostAsync($"/api/users/{id}/deactivate", null, Ct);

        // Assert - signed out at the next request; signing in gets the same answer as a wrong password
        deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredAsync(id)).IsActive.Should().BeFalse();
        await ShouldBeProblemAsync(await AsSessionAsync(session, HttpMethod.Get, "/api/auth/me"),
            HttpStatusCode.Unauthorized, "auth.signed_out");
        await ShouldBeProblemAsync(await SignInAsync(LenaEmail, LenaPassword),
            HttpStatusCode.Unauthorized, "auth.invalid_credentials");

        // Act - reactivated, they sign in again with their password
        var reactivated = await admin.PostAsync($"/api/users/{id}/reactivate", null, Ct);

        // Assert
        reactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SignInAsync(LenaEmail, LenaPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deactivate_OwnAccount_Returns409AndChangesNothing()
    {
        // The Admin manages every other account: deactivating it would lock the office out
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var adminId = await IdOfAsync(TestAdmin.Email);

        var response = await admin.PostAsync($"/api/users/{adminId}/deactivate", null, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "user.cannot_deactivate_self");
        (await StoredAsync(adminId)).IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("deactivate", false)]
    [InlineData("reactivate", true)]
    public async Task DeactivateOrReactivate_AlreadyInThatState_Returns204(string action, bool active)
    {
        // Arrange
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        await admin.PostAsync($"/api/users/{id}/{action}", null, Ct);

        // Act
        var response = await admin.PostAsync($"/api/users/{id}/{action}", null, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredAsync(id)).IsActive.Should().Be(active);
    }

    [Theory]
    [InlineData("deactivate")]
    [InlineData("reactivate")]
    public async Task DeactivateOrReactivate_UnknownId_Returns404(string action)
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);

        var response = await admin.PostAsync($"/api/users/999/{action}", null, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "user.not_found");
    }

    #endregion

    #region Changing your own password

    [Fact]
    public async Task ChangePassword_RightCurrentOne_ThisSessionGoesOnWithANewCookie_TheOthersEnd()
    {
        // Arrange - Lena has two sessions, both from a minute ago, and changes her password in one of them
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        var here = EarlierSession(id, UserRole.Technician);
        var elsewhere = EarlierSession(id, UserRole.Technician);

        // Act
        var response = await AsSessionAsync(here, HttpMethod.Put, "/api/auth/password",
            new ChangePasswordRequest { CurrentPassword = LenaPassword, NewPassword = "Inverter#77" });

        // Assert - the new cookie works; both earlier tokens, this one's included, are refused
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var renewed = SessionOf(response);
        (await AsSessionAsync(renewed, HttpMethod.Get, "/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        await ShouldBeProblemAsync(await AsSessionAsync(elsewhere, HttpMethod.Get, "/api/auth/me"),
            HttpStatusCode.Unauthorized, "auth.signed_out");
        await ShouldBeProblemAsync(await AsSessionAsync(here, HttpMethod.Get, "/api/auth/me"),
            HttpStatusCode.Unauthorized, "auth.signed_out");
        (await SignInAsync(LenaEmail, "Inverter#77")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentOne_Returns400OnThatField_AndTheSessionGoesOn()
    {
        // Arrange
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        var session = EarlierSession(id, UserRole.Technician);

        // Act
        var response = await AsSessionAsync(session, HttpMethod.Put, "/api/auth/password",
            new ChangePasswordRequest { CurrentPassword = "Wrong#2026", NewPassword = "Inverter#77" });

        // Assert - a field to fix, not a 401, which would end the session in the staff app
        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").EnumerateObject().Should().ContainSingle()
            .Which.Should().Match<JsonProperty>(p => p.Name == "CurrentPassword" &&
                                                     p.Value[0].GetString() == "The current password is incorrect.");
        response.Headers.Contains("Set-Cookie").Should().BeFalse();
        (await AsSessionAsync(session, HttpMethod.Get, "/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SignInAsync(LenaEmail, LenaPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_NewOneBreakingTheRule_Returns400OnNewPassword()
    {
        var technician = await CreateClientAsAsync(UserRole.Technician);

        var response = await technician.PutAsJsonAsync("/api/auth/password",
            new ChangePasswordRequest { CurrentPassword = "anything", NewPassword = "inverter#77" }, Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().Equal("NewPassword");
    }

    [Fact]
    public async Task ChangePassword_WithTheCookieFromAnotherSite_Returns403()
    {
        // The same-origin check (ADR 7) covers this change too: a page on another site can't send it
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await CreateAsync(admin, Lena());
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/auth/password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest { CurrentPassword = LenaPassword, NewPassword = "Inverter#77" })
        };
        request.Headers.Add("Cookie", $"solaris_session={EarlierSession(id, UserRole.Technician)}");
        request.Headers.Add("Origin", "https://elsewhere.example");

        var response = await CreateClient().SendAsync(request, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "auth.cross_site_request");
    }

    #endregion

    #region Signing in while the account changes

    [Fact]
    public async Task SignIn_AccountDeactivatedWhileThePasswordIsChecked_StaysDeactivated()
    {
        // Arrange - the Admin deactivates Lena in the moment between her sign-in reading the account and saving it
        var hasher = new DeactivatingHasher(CreateContext, LenaEmail);
        var admin = await CreateClientAsAsync(UserRole.Admin,
            services => services.AddSingleton<IPasswordHasher>(hasher));
        var id = await CreateAsync(admin, Lena());
        hasher.Armed = true;

        // Act
        var signIn = await SignInAsync(LenaEmail, LenaPassword);

        // Assert - the sign-in wrote only its time, so the deactivation stays, and the session it got ends at once
        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await StoredAsync(id);
        stored.IsActive.Should().BeFalse();
        stored.LastLoginAt.Should().NotBeNull();
        await ShouldBeProblemAsync(await AsSessionAsync(SessionOf(signIn), HttpMethod.Get, "/api/auth/me"),
            HttpStatusCode.Unauthorized, "auth.signed_out");
    }

    /// <summary>The real hasher, which deactivates one account, from another connection, while checking its password.</summary>
    private sealed class DeactivatingHasher(Func<global::Infrastructure.Data.AppDbContext> createContext, string email)
        : IPasswordHasher
    {
        private readonly PasswordHasher _real = new();

        public bool Armed { get; set; }

        public string Hash(string password) => _real.Hash(password);

        public void VerifyNothing(string password) => _real.VerifyNothing(password);

        public PasswordCheck Verify(string hash, string password)
        {
            if (Armed)
            {
                using var context = createContext();
                context.Users.Where(u => u.NormalizedEmail == User.NormalizeEmail(email))
                    .ExecuteUpdate(s => s.SetProperty(u => u.IsActive, false));
            }
            return _real.Verify(hash, password);
        }
    }

    #endregion
}
