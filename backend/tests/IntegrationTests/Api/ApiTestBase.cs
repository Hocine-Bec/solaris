using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Fakes;

namespace IntegrationTests.Api;

/// <summary>
/// Runs the real API in memory, on this test's own PostgreSQL database, outside Development by default (so errors
/// look as they do in production). Only the lead notifier is a fake, unless a test asks for the real one.
/// </summary>
public abstract class ApiTestBase(PostgresFixture database) : IntegrationTestBase(database)
{
    private WebApplicationFactory<Program>? _factory;

    protected RecordingLeadNotifier Notifier { get; } = new();

    /// <summary>The first Admin, created when the API starts on this test's empty database (FR-U3).</summary>
    protected static class TestAdmin
    {
        public const string Name = "Rosa Castillo";
        public const string Email = "rosa.castillo@solaris.test";
        public const string Password = "Solaris#2026";
    }

    /// <summary>
    /// A client for the API; <paramref name="configureServices"/> can replace services, <paramref name="environment"/>
    /// can change the environment, <paramref name="settings"/> can add settings and <paramref name="fakeNotifier"/>
    /// = false keeps the notifier of the configured mode, before the first call only.
    /// </summary>
    protected HttpClient CreateClient(Action<IServiceCollection>? configureServices = null,
        string environment = "Testing", IDictionary<string, string>? settings = null, bool fakeNotifier = true)
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
            builder.UseSetting("Serilog:MinimumLevel", "Warning");
            builder.UseSetting("Admin:Name", TestAdmin.Name);
            builder.UseSetting("Admin:Email", TestAdmin.Email);
            builder.UseSetting("Admin:Password", TestAdmin.Password);
            foreach (var (key, value) in settings ?? new Dictionary<string, string>())
                builder.UseSetting(key, value);
            builder.ConfigureTestServices(services =>
            {
                if (fakeNotifier)
                    services.AddSingleton<ILeadNotifier>(Notifier);
                RemoveHangfireServer(services);
                configureServices?.Invoke(services);
            });
        });
        return _factory.CreateClient();
    }

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

    /// <summary>Checks the response is a ProblemDetails with this status and code, and returns its body.</summary>
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
