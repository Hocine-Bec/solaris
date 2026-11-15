using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Api;

// The Admin's list of the emails that failed for good, and retrying them (FR-N2, BR-N2).
public class FailedNotificationsTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Whole microseconds, as PostgreSQL stores them, so a time read back equals the one seeded
    private static readonly DateTime Now = new(DateTime.UtcNow.Ticks / 10 * 10, DateTimeKind.Utc);

    /// <summary>
    /// A lead with its two messages, each in the given status: a Failed one failed <paramref name="failedAgo"/> ago (1 h by
    /// default), at the end of its 24 hours.
    /// </summary>
    private async Task<(Lead Lead, OutboxMessage Welcome, OutboxMessage Alert)> SeedLeadAsync(int n,
        OutboxMessageStatus welcome = OutboxMessageStatus.Failed, OutboxMessageStatus alert = OutboxMessageStatus.Failed,
        TimeSpan? failedAgo = null)
    {
        var address = AddressFactory.CreateFakeAddress(street: $"{n} Failed Ave");
        address.Id = 0;
        var lead = LeadFactory.CreateFakeLeadWithAddress(address: address);
        lead.Id = 0;
        lead.AddressId = 0;
        lead.Email = $"lead.{n}@example.com";
        var failedAt = Now - (failedAgo ?? TimeSpan.FromHours(1));
        var created = failedAt - OutboxMessage.DeliveryWindow;
        var messages = new[]
        {
            Apply(OutboxMessage.ForLead(lead, OutboxMessageType.LeadWelcome, created), welcome, failedAt),
            Apply(OutboxMessage.ForLead(lead, OutboxMessageType.SalesNewLeadAlert, created), alert, failedAt)
        };
        await SeedAsync(messages);
        return (lead, messages[0], messages[1]);
    }

    private static OutboxMessage Apply(OutboxMessage message, OutboxMessageStatus status, DateTime failedAt)
    {
        switch (status)
        {
            case OutboxMessageStatus.Failed:
                message.RecordFailure("The mail server smtp.gmail.com:587 can't be reached.", failedAt);
                break;
            case OutboxMessageStatus.Sent:
                message.MarkSent(message.CreatedAt.AddMinutes(1));
                break;
        }
        return message;
    }

    private async Task<OutboxMessage> RowAsync(Guid id) =>
        await Context.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == id, Ct);

    #region The list

    [Fact]
    public async Task FailedList_SaysWhichEmailToWhomAboutWhichLeadAndWhy_NewestFailureFirst()
    {
        // Arrange - an older failure, a newer one, and messages that aren't Failed
        var client = await CreateClientAsAsync(UserRole.Admin);
        var older = await SeedLeadAsync(1, alert: OutboxMessageStatus.Sent, failedAgo: TimeSpan.FromHours(3));
        var newer = await SeedLeadAsync(2, alert: OutboxMessageStatus.Pending);

        // Act
        var page = await client.GetFromJsonAsync<JsonElement>("/api/notifications/failed", Ct);

        // Assert
        page.GetProperty("totalCount").GetInt32().Should().Be(2);
        var items = page.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("id").GetGuid()).Should().Equal(newer.Welcome.Id, older.Welcome.Id);
        var first = items[0];
        first.GetProperty("type").GetString().Should().Be("LeadWelcome");
        first.GetProperty("to").GetProperty("address").GetString().Should().Be("lead.2@example.com");
        first.GetProperty("to").GetProperty("name").GetString().Should().Be($"{newer.Lead.FirstName} {newer.Lead.LastName}");
        first.GetProperty("lead").GetProperty("id").GetInt32().Should().Be(newer.Lead.Id);
        first.GetProperty("attempts").GetInt32().Should().Be(1);
        first.GetProperty("lastError").GetString().Should().Be("The mail server smtp.gmail.com:587 can't be reached.");
        first.GetProperty("retriedBy").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task FailedList_TheOfficesAlert_IsToTheOffice()
    {
        var client = await CreateClientAsAsync(UserRole.Admin);
        var seeded = await SeedLeadAsync(1, welcome: OutboxMessageStatus.Sent);

        var page = await client.GetFromJsonAsync<JsonElement>("/api/notifications/failed", Ct);

        var alert = page.GetProperty("items").EnumerateArray().Single();
        alert.GetProperty("type").GetString().Should().Be("SalesNewLeadAlert");
        alert.GetProperty("to").GetProperty("address").GetString().Should().Be("solaris.test@example.com");
        alert.GetProperty("to").GetProperty("name").GetString().Should().Be("Solaris");
        alert.GetProperty("lead").GetProperty("name").GetString().Should().Be($"{seeded.Lead.FirstName} {seeded.Lead.LastName}");
    }

    [Fact]
    public async Task FailedList_Paginated_LikeTheOthers()
    {
        var client = await CreateClientAsAsync(UserRole.Admin);
        for (var n = 1; n <= 3; n++)
            await SeedLeadAsync(n);

        var page = await client.GetFromJsonAsync<JsonElement>("/api/notifications/failed?page=2&pageSize=4", Ct);

        page.GetProperty("totalCount").GetInt32().Should().Be(6);
        page.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    #endregion

    #region Retrying one

    [Fact]
    public async Task BR_N2_Retry_FailedEmail_IsDueNowWithAnotherDay_AndRecordsWho()
    {
        // Arrange
        var client = await CreateClientAsAsync(UserRole.Admin);
        var seeded = await SeedLeadAsync(1);
        var before = DateTime.UtcNow;

        // Act
        var response = await client.PostAsync($"/api/notifications/{seeded.Welcome.Id}/retry", null, Ct);

        // Assert - the schedule starts over: Pending, due now, no attempts, 24 h from the retry
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var row = await RowAsync(seeded.Welcome.Id);
        (row.Status, row.Attempts).Should().Be((OutboxMessageStatus.Pending, 0));
        row.NextAttemptAt.Should().BeOnOrAfter(before.AddSeconds(-1)).And.BeOnOrBefore(DateTime.UtcNow);
        row.GiveUpAt.Should().Be(row.NextAttemptAt.AddHours(24));
        (row.RetriedAt, row.RetriedByUserId).Should().Be((row.NextAttemptAt, await IdOfAsync(TestAdmin.Email)));
        row.CreatedAt.Should().Be(seeded.Welcome.CreatedAt, "it keeps when it was first saved");
        (await RowAsync(seeded.Alert.Id)).Status.Should().Be(OutboxMessageStatus.Failed, "only that email is retried");
    }

    [Fact]
    public async Task Retry_UnknownEmail_Returns404()
    {
        var client = await CreateClientAsAsync(UserRole.Admin);

        var response = await client.PostAsync($"/api/notifications/{Guid.CreateVersion7()}/retry", null, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "notification.not_found");
    }

    [Theory]
    [InlineData(OutboxMessageStatus.Pending)]
    [InlineData(OutboxMessageStatus.Sent)]
    public async Task Retry_EmailThatIsntFailed_Returns409(OutboxMessageStatus status)
    {
        var client = await CreateClientAsAsync(UserRole.Admin);
        var seeded = await SeedLeadAsync(1, welcome: status);

        var response = await client.PostAsync($"/api/notifications/{seeded.Welcome.Id}/retry", null, Ct);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "notification.not_failed");
        (await RowAsync(seeded.Welcome.Id)).Status.Should().Be(status);
    }

    [Fact]
    public async Task Retry_TwoAdminsAtOnce_OneRetriesIt_TheOtherIsTold()
    {
        // Arrange - two requests for the same email, released at the same moment
        var client = await CreateClientAsAsync(UserRole.Admin);
        var seeded = await SeedLeadAsync(1);
        var start = new TaskCompletionSource();
        var requests = Enumerable.Range(0, 2).Select(async _ =>
        {
            await start.Task;
            return await client.PostAsync($"/api/notifications/{seeded.Welcome.Id}/retry", null, Ct);
        }).ToList();

        // Act
        start.SetResult();
        var responses = await Task.WhenAll(requests);

        // Assert
        responses.Select(r => r.StatusCode).Should()
            .BeEquivalentTo([HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
    }

    #endregion

    #region Retry all

    [Fact]
    public async Task RetryAll_RetriesTheFailedOnes_AndSaysHowMany()
    {
        // Arrange - four Failed (the first two leads' welcome emails, both of the third's), one Pending, one Sent
        var client = await CreateClientAsAsync(UserRole.Admin);
        var one = await SeedLeadAsync(1, alert: OutboxMessageStatus.Pending);
        var two = await SeedLeadAsync(2, alert: OutboxMessageStatus.Sent);
        await SeedLeadAsync(3);

        // Act
        var response = await client.PostAsync("/api/notifications/failed/retry", null, Ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("retried").GetInt32().Should().Be(4);
        var rows = await Context.OutboxMessages.AsNoTracking().ToListAsync(Ct);
        rows.Count(m => m.Status == OutboxMessageStatus.Pending && m.RetriedAt != null).Should().Be(4);
        rows.Single(m => m.Id == one.Alert.Id).RetriedAt.Should().BeNull("it wasn't Failed");
        rows.Single(m => m.Id == two.Alert.Id).Status.Should().Be(OutboxMessageStatus.Sent);
    }

    [Fact]
    public async Task RetryAll_NothingFailed_RetriesNothing()
    {
        var client = await CreateClientAsAsync(UserRole.Admin);

        var response = await client.PostAsync("/api/notifications/failed/retry", null, Ct);

        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("retried").GetInt32().Should().Be(0);
    }

    #endregion

    [Fact]
    public void CleanupJob_IsScheduledDaily()
    {
        CreateClient();

        var jobs = ApiServices.GetRequiredService<JobStorage>().GetConnection().GetRecurringJobs();

        jobs.Should().ContainSingle(j => j.Id == "outbox-cleanup").Which.Cron.Should().Be(Cron.Daily());
    }
}
