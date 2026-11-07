using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// The measurement harness's fault toggle (docs/measurements.md §4): Development only.
public class MeasurementEndpointTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record EmailFaultState(bool On);

    [Fact]
    public async Task EmailFault_InDevelopment_IsOffAtStartupAndCanBeTurnedOnAndOff()
    {
        var client = CreateClient(environment: "Development");

        (await client.GetFromJsonAsync<EmailFaultState>("/api/dev/email-fault", Ct))!.On.Should().BeFalse();

        (await client.PutAsJsonAsync("/api/dev/email-fault", new EmailFaultState(true), Ct))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<EmailFaultState>("/api/dev/email-fault", Ct))!.On.Should().BeTrue();

        (await client.PutAsJsonAsync("/api/dev/email-fault", new EmailFaultState(false), Ct))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<EmailFaultState>("/api/dev/email-fault", Ct))!.On.Should().BeFalse();
    }

    [Fact]
    public async Task EmailFault_FaultInjectionEnabled_IsOnAtStartup()
    {
        // P2 scenario C restarts the API while the mail server is down: the restarted API must start with the fault.
        var client = CreateClient(environment: "Development",
            settings: new Dictionary<string, string> { ["FaultInjection:Enabled"] = "true" });

        (await client.GetFromJsonAsync<EmailFaultState>("/api/dev/email-fault", Ct))!.On.Should().BeTrue();
    }

    [Fact]
    public async Task EmailFault_OutsideDevelopment_DoesNotExist()
    {
        var response = await CreateClient().GetAsync("/api/dev/email-fault", Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "not_found");
    }
}
