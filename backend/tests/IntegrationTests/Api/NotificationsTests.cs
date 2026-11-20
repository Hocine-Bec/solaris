using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Models;
using Application.Interfaces.Services;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// A new lead's two emails through the running API (BR-N1, BR-N2): the request saves them in the outbox and answers;
// the dispatcher, a background service of the API, sends them.
public class NotificationsTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NewLead_TheDispatcherSendsBothEmails_AfterTheRequestHasAnswered()
    {
        // Arrange - the mail server is a stand-in; the dispatcher runs
        var accepted = new ConcurrentQueue<EmailMessage>();
        var client = CreateClient(services => services.AddScoped<IEmailSender>(_ => new AcceptingSender(accepted)),
            dispatcher: true);

        // Act
        var response = await client.PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetInt32();

        // Assert - both sent within a few of the dispatcher's 2 s rounds
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (accepted.Count < 2 && DateTime.UtcNow < deadline)
            await Task.Delay(100, Ct);
        accepted.Select(m => m.NotificationKey).Should().BeEquivalentTo($"LeadWelcome:{id}", $"SalesNewLeadAlert:{id}");
        var rows = await Context.OutboxMessages.AsNoTracking().ToListAsync(Ct);
        rows.Should().HaveCount(2).And.AllSatisfy(m =>
            (m.LeadId, m.Status, m.Attempts).Should().Be((id, OutboxMessageStatus.Sent, 1)));
    }

    [Fact]
    public async Task NewLead_WithoutTheDispatcher_TheRequestStillAnswers_AndBothWait()
    {
        // The request never talks to the mail server: here, nothing sends at all
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Context.OutboxMessages.AsNoTracking().Select(m => m.Status).ToListAsync(Ct))
            .Should().Equal(OutboxMessageStatus.Pending, OutboxMessageStatus.Pending);
    }

    private sealed class AcceptingSender(ConcurrentQueue<EmailMessage> accepted) : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            accepted.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
