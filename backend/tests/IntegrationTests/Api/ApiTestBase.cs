using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Hangfire;
using Infrastructure.Outbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestUtilities.Base;
using TestUtilities.Database;

namespace IntegrationTests.Api;

/// <summary>
/// Runs the real API in memory, on this test's own PostgreSQL database, outside Development by default (so errors
/// look as they do in production). The background services that send email (the outbox dispatcher) and run jobs
/// (Hangfire's server) don't run, unless a test asks for the dispatcher: a new lead's outbox messages stay Pending.
/// </summary>
public abstract class ApiTestBase(PostgresFixture database) : IntegrationTestBase(database)
{
    private WebApplicationFactory<Program>? _factory;

    /// <summary>The first Admin, created when the API starts on this test's empty database (FR-U3).</summary>
    protected static class TestAdmin
    {
        public const string Name = "Rosa Castillo";
        public const string Email = "rosa.castillo@solaris.test";
        public const string Password = "Solaris#2026";
    }

    /// <summary>
    /// A client for the API; <paramref name="configureServices"/> can replace services, <paramref name="environment"/>
    /// can change the environment, <paramref name="settings"/> can add settings and <paramref name="dispatcher"/> =
    /// true runs the outbox dispatcher, before the first call only.
    /// </summary>
    protected HttpClient CreateClient(Action<IServiceCollection>? configureServices = null,
        string environment = "Testing", IDictionary<string, string>? settings = null, bool dispatcher = false)
    {
        _factory ??= new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Default", ConnectionString);
            builder.UseSetting("Jwt:Secret", "test-secret-test-secret-test-secret-test-secret");
            builder.UseSetting("Email:FromAddress", "solaris.test@example.com");
            builder.UseSetting("Email:SiteUrl", "https://solaris.test");
            builder.UseSetting("Email:Gmail:ClientId", "test-client-id");
            builder.UseSetting("Email:Gmail:ClientSecret", "test-client-secret");
            builder.UseSetting("Email:Gmail:RefreshToken", "test-refresh-token");
            builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
            builder.UseSetting("Admin:Name", TestAdmin.Name);
            builder.UseSetting("Admin:Email", TestAdmin.Email);
            builder.UseSetting("Admin:Password", TestAdmin.Password);
            foreach (var (key, value) in settings ?? new Dictionary<string, string>())
                builder.UseSetting(key, value);
            builder.ConfigureTestServices(services =>
            {
                RemoveHangfireServer(services);
                if (!dispatcher)
                    services.Remove(services.Single(d => d.ImplementationType == typeof(OutboxDispatcher)));
                configureServices?.Invoke(services);
            });
        });
        return _factory.CreateClient();
    }

    /// <summary>The running API's services, once <see cref="CreateClient"/> has started it.</summary>
    protected IServiceProvider ApiServices =>
        _factory?.Services ?? throw new InvalidOperationException("Call CreateClient first: the API isn't running.");

    /// <summary>A technician's account, added to this test's database by <see cref="CreateClientAsAsync"/>.</summary>
    protected static class TestTechnician
    {
        public const string Name = "Jamal Ortiz";
        public const string Email = "jamal.ortiz@solaris.test";
    }

    /// <summary>
    /// A client signed in as the test Admin, or as a technician added here, its session's token in the Authorization
    /// header, as the API reference and scripts send it. The token is issued directly, without signing in, so no
    /// password is hashed. The other parameters are <see cref="CreateClient"/>'s.
    /// </summary>
    protected async Task<HttpClient> CreateClientAsAsync(UserRole role,
        Action<IServiceCollection>? configureServices = null, string environment = "Testing")
    {
        var client = CreateClient(configureServices, environment);
        await using var scope = _factory!.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepo>();
        var user = role == UserRole.Admin
            ? await users.GetByEmailAsync(TestAdmin.Email)
            : await users.GetByEmailAsync(TestTechnician.Email) ?? await AddTechnicianAsync(scope.ServiceProvider);
        var (token, _) = scope.ServiceProvider.GetRequiredService<ISessionTokenIssuer>().Issue(user!);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>The ID of the account with this email, read from this test's database.</summary>
    protected Task<int> IdOfAsync(string email) =>
        Context.Users.Where(u => u.NormalizedEmail == User.NormalizeEmail(email)).Select(u => u.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

    private static async Task<User> AddTechnicianAsync(IServiceProvider services)
    {
        var technician = new User
        {
            Name = TestTechnician.Name, Email = TestTechnician.Email,
            NormalizedEmail = User.NormalizeEmail(TestTechnician.Email), PasswordHash = "not used: no test signs in with it",
            Role = UserRole.Technician, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        services.GetRequiredService<IUserRepo>().Add(technician);
        await services.GetRequiredService<IUnitOfWork>().SaveAsync();
        return technician;
    }

    // No test here enqueues a job (the notifier is a fake, or synchronous), and a Hangfire server keeps up to 20
    // workers polling the database: with several test classes running at once, PostgreSQL ran out of connections
    // ("53300: sorry, too many clients already").
    private static void RemoveHangfireServer(IServiceCollection services)
    {
        var server = services.Where(d => d.ServiceType == typeof(IHostedService) &&
            d.ImplementationFactory?.Method.ReturnType == typeof(BackgroundJobServerHostedService)).ToList();
        server.Should().ContainSingle("the API registers one Hangfire server, which these tests don't start");
        services.Remove(server[0]);
    }

    public override async ValueTask DisposeAsync()
    {
        // Stop the API (and its Hangfire server) before the database is dropped.
        if (_factory is not null)
            await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Lets each request say which address it comes from, in its X-Test-Peer header: the API in memory has no connection
    /// address of its own. Pass it as <see cref="CreateClient"/>'s configureServices.
    /// </summary>
    protected static readonly Action<IServiceCollection> WithTestPeers =
        services => services.AddSingleton<IStartupFilter, TestPeerAddress>();

    /// <summary>Gives each request the connection address in its X-Test-Peer header, before the API's middleware runs.</summary>
    protected sealed class TestPeerAddress : IStartupFilter
    {
        public const string Header = "X-Test-Peer";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(Header, out var peer))
                    context.Connection.RemoteIpAddress = IPAddress.Parse(peer.ToString());
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    /// <summary>Checks the response is a ProblemDetails with this status and code, and returns its body.</summary>
    // ---- Walking the screens' own flows, for the tests that need an installation to exist ----

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    protected static async Task<JsonElement> OkJsonAsync(Task<HttpResponseMessage> call)
    {
        var response = await call;
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return await JsonAsync(response);
    }

    /// <summary>A lead from the form, contacted and converted by the Admin: the new installation's id.</summary>
    protected async Task<int> ConvertedAsync(HttpClient admin, string email = "jane.doe@example.com", string street = "100 Congress Ave")
    {
        var lead = await JsonAsync(await CreateClient().PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(email, street), Ct));
        var id = lead.GetProperty("id").GetInt32();
        await OkJsonAsync(admin.PutAsJsonAsync($"/api/leads/{id}/status", new { status = "Contacted" }, Ct));
        var converted = await OkJsonAsync(admin.PostAsync($"/api/leads/{id}/convert", null, Ct));
        return converted.GetProperty("installationId").GetInt32();
    }

    /// <summary>
    /// A change as the staff app sends it (BR-I4): with the version of the installation it last read, its ETag, as
    /// If-Match. A technician reads their installation from My work.
    /// </summary>
    protected static async Task<HttpResponseMessage> Change(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var read = await client.GetAsync(Regex.Match(path, @"^/api/(installations|my-work)/\d+").Value, Ct);
        return await Send(client, method, path, body, read.Headers.ETag?.Tag);
    }

    protected static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object? body, string? ifMatch)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        if (ifMatch is not null)
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return client.SendAsync(request, Ct);
    }

    protected static Task<JsonElement> Move(HttpClient admin, int id, string to) =>
        OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{id}/status", new { status = to }));

    protected static Task<JsonElement> Assign(HttpClient admin, int id, int technicianId, string role) =>
        OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{id}/assignments", new { technicianId, role }));

    protected static int AssignmentId(JsonElement installation, string role) =>
        installation.GetProperty("assignments").EnumerateArray().First(a => a.GetProperty("role").GetString() == role)
            .GetProperty("id").GetInt32();

    /// <summary>An installation walked from conversion to Inspection, every part completed, then switched on.</summary>
    protected async Task<(HttpClient Admin, int Id, HttpResponseMessage SwitchedOn)> SwitchedOnAsync(string street = "100 Congress Ave")
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        await CreateClientAsAsync(UserRole.Technician);
        var techId = await IdOfAsync(TestTechnician.Email);
        var id = await ConvertedAsync(admin, street: street);
        foreach (var (role, then) in new[] { ("Surveyor", "Design"), ("Installer", "Installation"), ("Inspector", "") })
        {
            if (role == "Installer")
            {
                await OkJsonAsync(Change(admin, HttpMethod.Put, $"/api/installations/{id}/design", new { systemSizeKw = 8.4m, panelCount = 21, inverterType = "Enphase IQ8M" }));
                await Move(admin, id, "Permits");
            }
            if (role == "Inspector")
                await Move(admin, id, "Inspection");
            var page = await Assign(admin, id, techId, role);
            if (role == "Installer")
                await Move(admin, id, "Installation");
            await OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{id}/assignments/{AssignmentId(page, role)}/complete"));
            if (role == "Surveyor")
                await Move(admin, id, then);
        }
        return (admin, id, await Change(admin, HttpMethod.Post, $"/api/installations/{id}/status", new { status = "Active" }));
    }

    protected static async Task<JsonElement> ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status,
        string code)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        body.GetProperty("status").GetInt32().Should().Be((int)status);
        body.GetProperty("code").GetString().Should().Be(code);
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        return body;
    }
}
