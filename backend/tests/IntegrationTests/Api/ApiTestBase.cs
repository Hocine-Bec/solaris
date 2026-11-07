using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces.Services;
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
/// Runs the real API in memory, on this test's own PostgreSQL database, outside Development (so errors look as they
/// do in production). Only the email queue is a fake.
/// </summary>
public abstract class ApiTestBase(PostgresFixture database) : IntegrationTestBase(database)
{
    private WebApplicationFactory<Program>? _factory;

    protected RecordingEmailQueueHandler Emails { get; } = new();

    /// <summary>A client for the API; <paramref name="configureServices"/> can replace services, before the first call only.</summary>
    protected HttpClient CreateClient(Action<IServiceCollection>? configureServices = null)
    {
        _factory ??= new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", ConnectionString);
            builder.UseSetting("Jwt:Secret", "test-secret-test-secret-test-secret-test-secret");
            builder.UseSetting("Email:FromAddress", "solaris.test@example.com");
            builder.UseSetting("Email:Gmail:ClientId", "test-client-id");
            builder.UseSetting("Email:Gmail:ClientSecret", "test-client-secret");
            builder.UseSetting("Email:Gmail:RefreshToken", "test-refresh-token");
            builder.UseSetting("Serilog:MinimumLevel", "Warning");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IEmailQueueHandler>(Emails);
                RemoveHangfireServer(services);
                configureServices?.Invoke(services);
            });
        });
        return _factory.CreateClient();
    }

    // No test here enqueues a job (the email queue is a fake), and a Hangfire server keeps up to 20 workers polling
    // the database: with several test classes running at once, PostgreSQL ran out of connections ("53300: sorry,
    // too many clients already").
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
