using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// The P1 "before" (docs/measurements.md §6): with Notifications:Mode=Synchronous, in Development, the lead request
// sends both emails itself before it answers.
public class SynchronousNotificationsTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateLead_SynchronousMode_SendsBothEmailsBeforeAnswering()
    {
        // Arrange - the real notifier of the mode, and each email takes 250 ms
        var client = CreateClient(environment: "Development", fakeNotifier: false,
            settings: new Dictionary<string, string>
            {
                ["Notifications:Mode"] = "Synchronous",
                ["Email:SimulatedLatencyMs"] = "250"
            });
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await client.PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct);
        stopwatch.Stop();

        // Assert - when the 201 arrives, both emails are already in the delivery log, and the request took at least
        // the two sends (450 ms leaves room for the timer's precision)
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetInt32();
        var keys = await Context.EmailDeliveries.AsNoTracking().OrderBy(d => d.Id).Select(d => d.NotificationKey)
            .ToListAsync(Ct);
        keys.Should().Equal($"LeadWelcome:{id}", $"SalesNewLeadAlert:{id}");
        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(450));
    }
}
