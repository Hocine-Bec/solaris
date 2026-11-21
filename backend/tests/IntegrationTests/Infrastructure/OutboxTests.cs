using System.Collections.Concurrent;
using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Outbox;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TestUtilities.Base;
using TestUtilities.Database;
using TestUtilities.Factories;
using TestUtilities.Fakes;

namespace IntegrationTests.Infrastructure;

// The outbox dispatcher's batches on PostgreSQL (BR-N2): what is sent, the retry schedule, the 24 hours, a mail server
// that can't be reached, a shutdown mid-batch, and two dispatchers at once. The clock is the test's; the mail server
// is a stand-in that records what it accepts.
public class OutboxTests(PostgresFixture database) : IntegrationTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero));

    private static readonly IOptions<EmailOptions> Settings = Options.Create(new EmailOptions
    {
        FromName = "Solaris", FromAddress = "office@example.com", SiteUrl = "https://solaris.test"
    });

    private OutboxProcessor Processor(IEmailSender sender, AppDbContext? context = null, int maxConcurrentSends = 5) =>
        new(context ?? CreateContext(), new EmailService(sender, Settings), _clock,
            Options.Create(new OutboxOptions { MaxConcurrentSends = maxConcurrentSends }),
            NullLogger<OutboxProcessor>.Instance);

    /// <summary>Leads, each with its two messages, created at <paramref name="createdAt"/> (the clock's time by default).</summary>
    private async Task<List<OutboxMessage>> SeedLeadsAsync(int count, DateTime? createdAt = null)
    {
        var messages = new List<OutboxMessage>();
        for (var n = 1; n <= count; n++)
        {
            var address = AddressFactory.CreateFakeAddress(street: $"{n} Outbox Ave");
            address.Id = 0;
            var lead = LeadFactory.CreateFakeLeadWithAddress(address: address);
            lead.Id = 0;
            lead.AddressId = 0;
            messages.Add(OutboxMessage.ForLead(lead, OutboxMessageType.LeadWelcome, createdAt ?? _clock.UtcNow));
            messages.Add(OutboxMessage.ForLead(lead, OutboxMessageType.SalesNewLeadAlert, createdAt ?? _clock.UtcNow));
        }
        await SeedAsync(messages.ToArray());
        return messages;
    }

    private Task<List<OutboxMessage>> RowsAsync() =>
        Context.OutboxMessages.AsNoTracking().OrderBy(m => m.Id).ToListAsync(Ct);

    #region Sending

    [Fact]
    public async Task Batch_DueMessages_AreSentWithTheirKeysAndIds_AndMarkedSent()
    {
        // Arrange
        var seeded = await SeedLeadsAsync(2);
        var sender = new RecordingSender();

        // Act
        var batch = await Processor(sender).SendDueAsync(Ct);

        // Assert - each email names its notification and carries its message's id (the Message-ID)
        batch.Should().Be(new OutboxBatch(4, 4, false));
        sender.Accepted.Select(m => (m.NotificationKey, m.OutboxMessageId)).Should()
            .BeEquivalentTo(seeded.Select(m => (m.NotificationKey, (Guid?)m.Id)));
        (await RowsAsync()).Should().AllSatisfy(m =>
        {
            (m.Status, m.Attempts, m.SentAt).Should().Be((OutboxMessageStatus.Sent, 1, _clock.UtcNow));
        });
    }

    [Fact]
    public async Task FR_I7_SwitchOnEmail_IsSentToTheCustomer_WithItsKeyAndId()
    {
        // An installation switched on, its message saved with the move
        var address = AddressFactory.CreateFakeAddress(street: "61 Pecan St", state: "TX");
        address.Id = 0;
        var lead = LeadFactory.CreateFakeLeadWithAddress(address: address);
        lead.Id = 0;
        var installation = new Installation
        {
            Lead = lead, Address = address, Status = InstallationStatus.Active, TimeZone = "America/Chicago",
            SystemSizeKw = 8.4m, PanelCount = 21, CreatedAt = _clock.UtcNow, StatusChangedAt = _clock.UtcNow, ActivatedAt = _clock.UtcNow,
            Customer = new Customer
            {
                FirstName = "Grace", LastName = "Kim", Email = "grace.kim@example.com", NormalizedEmail = "GRACE.KIM@EXAMPLE.COM",
                PhoneNumber = "(512) 555-0110", Status = CustomerStatus.Active, RegistrationDate = _clock.UtcNow
            }
        };
        var message = OutboxMessage.ForInstallation(installation, OutboxMessageType.InstallationActivated, _clock.UtcNow);
        await SeedAsync(message);
        var sender = new RecordingSender();

        var batch = await Processor(sender).SendDueAsync(Ct);

        batch.Should().Be(new OutboxBatch(1, 1, false));
        sender.Accepted.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            NotificationKey = $"InstallationActivated:{installation.Id}", OutboxMessageId = (Guid?)message.Id,
            ToAddress = "grace.kim@example.com", Subject = "Your solar system is on"
        }, o => o.ExcludingMissingMembers());
        (await RowsAsync()).Single().Status.Should().Be(OutboxMessageStatus.Sent);
    }

    [Fact]
    public async Task Batch_SentOnce_IsNotTakenAgain()
    {
        await SeedLeadsAsync(1);
        var sender = new RecordingSender();
        await Processor(sender).SendDueAsync(Ct);

        var again = await Processor(sender).SendDueAsync(Ct);

        again.Taken.Should().Be(0);
        sender.Accepted.Should().HaveCount(2);
    }

    [Fact]
    public async Task Batch_TakesAtMostTwenty_AndSaysWhenThereIsMore()
    {
        await SeedLeadsAsync(11);

        var first = await Processor(new RecordingSender()).SendDueAsync(Ct);
        var second = await Processor(new RecordingSender()).SendDueAsync(Ct);

        (first.Taken, first.TakeMoreNow).Should().Be((20, true));
        (second.Taken, second.TakeMoreNow).Should().Be((2, false));
    }

    #endregion

    #region Failures (BR-N2)

    [Fact]
    public async Task BR_N2_OneEmailFails_ItWaitsTenSeconds_AndTheOthersAreStillSent()
    {
        // Arrange - the mail server refuses one email; it answers, so the batch goes on
        var seeded = await SeedLeadsAsync(2);
        var refused = seeded[0];
        var sender = new RecordingSender(m => m.OutboxMessageId == refused.Id
            ? throw new InvalidOperationException("550 Mailbox unavailable.")
            : Task.CompletedTask);

        // Act
        var batch = await Processor(sender).SendDueAsync(Ct);

        // Assert
        batch.Should().Be(new OutboxBatch(4, 3, false));
        var rows = await RowsAsync();
        var failed = rows.Single(m => m.Id == refused.Id);
        (failed.Status, failed.Attempts, failed.NextAttemptAt, failed.LastError).Should().Be(
            (OutboxMessageStatus.Pending, 1, _clock.UtcNow.AddSeconds(10), "550 Mailbox unavailable."));
        rows.Where(m => m.Id != refused.Id).Should().AllSatisfy(m => m.Status.Should().Be(OutboxMessageStatus.Sent));
    }

    [Fact]
    public async Task BR_N2_EveryAttemptFails_TheScheduleIsFollowed()
    {
        // Arrange
        await SeedAsync(AddressFactory.CreateFakeAddress(id: 1));
        var lead = LeadFactory.CreateFakeLead(id: 1, addressId: 1);
        await SeedAsync(lead);
        await using (var context = CreateContext())
        {
            context.OutboxMessages.Add(OutboxMessage.ForLead(await context.Leads.SingleAsync(Ct),
                OutboxMessageType.LeadWelcome, _clock.UtcNow));
            await context.SaveChangesAsync(Ct);
        }
        var sender = new RecordingSender(_ => throw new MailServerUnavailableException("Down."));
        var waits = new List<TimeSpan>();

        // Act - four batches, each when the message is due again
        for (var i = 0; i < 4; i++)
        {
            var before = _clock.UtcNow;
            await Processor(sender).SendDueAsync(Ct);
            var row = (await RowsAsync()).Single();
            waits.Add(row.NextAttemptAt - before);
            _clock.Now = row.NextAttemptAt;
        }

        // Assert
        waits.Should().Equal(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task BR_N2_FailsOnceTheDayHasPassed_IsFailed_AndNotTakenAgain()
    {
        // Arrange - messages from a day ago, retried until now
        await SeedLeadsAsync(1, createdAt: _clock.UtcNow.AddHours(-24));
        var sender = new RecordingSender(_ => throw new MailServerUnavailableException("Down."));

        // Act - the mail server down stops the first batch after one message; the other waits 10 s with it
        await Processor(sender).SendDueAsync(Ct);
        var afterFirst = await RowsAsync();
        _clock.Advance(TimeSpan.FromSeconds(10));
        await Processor(sender).SendDueAsync(Ct);
        _clock.Advance(TimeSpan.FromHours(1));
        var later = await Processor(sender).SendDueAsync(Ct);

        // Assert
        afterFirst.Select(m => (m.Status, m.Attempts)).Should().BeEquivalentTo(new[]
        {
            (OutboxMessageStatus.Failed, 1), (OutboxMessageStatus.Pending, 0)
        });
        (await RowsAsync()).Should().AllSatisfy(m =>
            (m.Status, m.Attempts, m.LastError).Should().Be((OutboxMessageStatus.Failed, 1, "Down.")));
        later.Taken.Should().Be(0);
    }

    [Fact]
    public async Task MailServerUnreachable_StopsTheBatch_AndTheRestWaitWithoutAnAttempt()
    {
        // Arrange
        await SeedLeadsAsync(2);
        var sender = new RecordingSender(_ => throw new MailServerUnavailableException("Connection refused."));

        // Act
        var batch = await Processor(sender).SendDueAsync(Ct);

        // Assert - one attempt reached the mail server; all four are due again in 10 s
        batch.Should().Be(new OutboxBatch(4, 0, true));
        batch.TakeMoreNow.Should().BeFalse();
        sender.Calls.Should().Be(1);
        var rows = await RowsAsync();
        rows.Should().AllSatisfy(m =>
            (m.Status, m.NextAttemptAt).Should().Be((OutboxMessageStatus.Pending, _clock.UtcNow.AddSeconds(10))));
        rows.Sum(m => m.Attempts).Should().Be(1);
    }

    [Fact]
    public async Task SendTakesTooLong_CountsAsAFailedAttempt()
    {
        await SeedLeadsAsync(1);
        var sender = new RecordingSender((_, ct) => Task.Delay(Timeout.Infinite, ct));

        var batch = await new OutboxProcessor(CreateContext(), new EmailService(sender, Settings), _clock,
            Options.Create(new OutboxOptions()), NullLogger<OutboxProcessor>.Instance)
        {
            SendTimeout = TimeSpan.FromMilliseconds(200)
        }.SendDueAsync(Ct);

        batch.Sent.Should().Be(0);
        (await RowsAsync()).Should().AllSatisfy(m => (m.Attempts, m.LastError)
            .Should().Be((1, "The mail server didn't answer within 0.2 s.")));
    }

    [Fact]
    public async Task MailServerUnreachable_WithSeveralSendsUnderWay_TheyCount_AndNoMoreStart()
    {
        // Arrange - five sends at once, each failing after 50 ms
        await SeedLeadsAsync(10);
        var sender = new RecordingSender(async (_, ct) =>
        {
            await Task.Delay(50, ct);
            throw new MailServerUnavailableException("Connection refused.");
        });

        // Act
        var batch = await Processor(sender, maxConcurrentSends: 5).SendDueAsync(Ct);

        // Assert - the five under way each count an attempt; the other fifteen weren't tried; all wait 10 s
        (batch.Taken, batch.Sent, batch.MailServerUnreachable).Should().Be((20, 0, true));
        sender.Calls.Should().Be(5);
        var rows = await RowsAsync();
        rows.Count(m => m.Attempts == 1).Should().Be(5);
        rows.Count(m => m.Attempts == 0).Should().Be(15);
        rows.Should().AllSatisfy(m => m.NextAttemptAt.Should().Be(_clock.UtcNow.AddSeconds(10)));
    }

    #endregion

    #region At once (Outbox:MaxConcurrentSends)

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(20)]
    public async Task Batch_SendsUpToTheSettingAtOnce_NeverMore(int maxConcurrentSends)
    {
        // Arrange - each send takes 50 ms, so the sends of a batch overlap as far as the setting allows
        await SeedLeadsAsync(10);
        var underWay = 0;
        var most = 0;
        var sender = new RecordingSender(async (_, ct) =>
        {
            var now = Interlocked.Increment(ref underWay);
            InterlockedMax(ref most, now);
            await Task.Delay(50, ct);
            Interlocked.Decrement(ref underWay);
        });

        // Act
        var batch = await Processor(sender, maxConcurrentSends: maxConcurrentSends).SendDueAsync(Ct);

        // Assert
        batch.Sent.Should().Be(20);
        most.Should().Be(maxConcurrentSends);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value &&
               Interlocked.CompareExchange(ref target, value, current) != current) { }
    }

    #endregion

    #region A retry (FR-N2) and the cleanup

    [Fact]
    public async Task BR_N2_RetriedEmail_TheScheduleStartsOver_AndItFailsOnlyAfterAnotherDay()
    {
        // Arrange - an email Failed at the end of its day, then retried by the Admin
        var seeded = await SeedLeadsAsync(1, createdAt: _clock.UtcNow.AddHours(-24));
        var failing = new RecordingSender(_ => throw new InvalidOperationException("Still down."));
        await Processor(failing).SendDueAsync(Ct);
        (await RowsAsync()).Should().AllSatisfy(m => m.Status.Should().Be(OutboxMessageStatus.Failed));
        var admin = UserRepoTests.NewUser("rosa.castillo@example.com");
        await SeedAsync(admin);
        var retried = await new OutboxRepo(CreateContext()).RetryFailedAsync(seeded[0].Id, _clock.UtcNow, admin.Id);

        // Act - the next batch, and the end of the new day
        await Processor(failing).SendDueAsync(Ct);
        var afterFirst = (await RowsAsync()).Single(m => m.Id == seeded[0].Id);
        _clock.Now = afterFirst.GiveUpAt;
        await Processor(failing).SendDueAsync(Ct);
        var atTheEnd = (await RowsAsync()).Single(m => m.Id == seeded[0].Id);

        // Assert - 10 s after the retry, as a new email; Failed only once the new 24 hours have passed
        retried.Should().Be(1);
        (afterFirst.Status, afterFirst.Attempts, afterFirst.NextAttemptAt - afterFirst.RetriedAt)
            .Should().Be((OutboxMessageStatus.Pending, 1, TimeSpan.FromSeconds(10)));
        afterFirst.GiveUpAt.Should().Be(afterFirst.RetriedAt!.Value.AddHours(24));
        (atTheEnd.Status, atTheEnd.Attempts, atTheEnd.RetriedByUserId)
            .Should().Be((OutboxMessageStatus.Failed, 2, admin.Id));
        var (listed, _) = await new OutboxRepo(CreateContext()).GetFailedPageAsync(0, 10);
        listed.Single(m => m.Id == seeded[0].Id).RetriedBy!.Name.Should().Be("Rosa Castillo");
    }

    [Fact]
    public async Task CleanupJob_DeletesTheEmailsSentMoreThanSevenDaysAgo_Only()
    {
        // Arrange - three leads' messages: sent 8 days ago, sent 6 days ago, and one Failed 8 days ago with one Pending
        var seeded = await SeedLeadsAsync(3, createdAt: _clock.UtcNow.AddDays(-9));
        await using (var context = CreateContext())
        {
            var rows = await context.OutboxMessages.OrderBy(m => m.LeadId).ThenBy(m => m.Type).ToListAsync(Ct);
            rows[0].MarkSent(_clock.UtcNow.AddDays(-8));
            rows[1].MarkSent(_clock.UtcNow.AddDays(-8));
            rows[2].MarkSent(_clock.UtcNow.AddDays(-6));
            rows[3].MarkSent(_clock.UtcNow.AddDays(-6));
            rows[4].RecordFailure("Down.", _clock.UtcNow.AddDays(-8));
            await context.SaveChangesAsync(Ct);
        }

        // Act - one row per statement, so the job needs several
        var deleted = await new OutboxCleanupJob(CreateContext(), _clock, NullLogger<OutboxCleanupJob>.Instance)
            { BatchSize = 1 }.RunAsync(Ct);

        // Assert
        deleted.Should().Be(2);
        (await RowsAsync()).Select(m => m.Status).Should().BeEquivalentTo(new[]
        {
            OutboxMessageStatus.Sent, OutboxMessageStatus.Sent, OutboxMessageStatus.Failed, OutboxMessageStatus.Pending
        });
        seeded.Should().HaveCount(6);
    }

    #endregion

    #region Stopping, and two dispatchers

    [Fact]
    public async Task StoppingMidBatch_KeepsWhatWasSent_AndTheRestStaysDueWithoutAnAttempt()
    {
        // Arrange - the host shuts down while the second email is being sent
        await SeedLeadsAsync(2);
        using var stopping = new CancellationTokenSource();
        var calls = 0;
        var stopOnSecond = new RecordingSender(async (_, ct) =>
        {
            if (Interlocked.Increment(ref calls) == 2)
            {
                await stopping.CancelAsync();
                ct.ThrowIfCancellationRequested();
            }
        });

        // Act
        var batch = await Processor(stopOnSecond).SendDueAsync(stopping.Token);

        // Assert - the first stays Sent; the others weren't tried, so nothing counts against them
        batch.Sent.Should().Be(1);
        var rows = await RowsAsync();
        rows.Count(m => m.Status == OutboxMessageStatus.Sent).Should().Be(1);
        rows.Where(m => m.Status == OutboxMessageStatus.Pending).Should().HaveCount(3)
            .And.AllSatisfy(m => (m.Attempts, m.NextAttemptAt).Should().Be((0, _clock.UtcNow)));
    }

    [Fact]
    public async Task TwoDispatchersAtOnce_NeverSendTheSameMessage()
    {
        // Arrange - 60 messages, each send taking 10 ms, so the two dispatchers' batches overlap
        await SeedLeadsAsync(30);
        var sender = new RecordingSender((_, ct) => Task.Delay(10, ct));
        var start = new TaskCompletionSource();

        async Task<int> RunAsync()
        {
            await start.Task;
            var sent = 0;
            OutboxBatch batch;
            do
            {
                await using var context = CreateContext();
                batch = await Processor(sender, context).SendDueAsync(Ct);
                sent += batch.Sent;
            } while (batch.Taken > 0);
            return sent;
        }

        // Act - each with its own connection, released at the same moment
        var first = Task.Run(RunAsync, Ct);
        var second = Task.Run(RunAsync, Ct);
        start.SetResult();
        var sentBy = await Task.WhenAll(first, second);

        // Assert
        sender.Accepted.Select(m => m.OutboxMessageId).Should().HaveCount(60).And.OnlyHaveUniqueItems();
        sentBy.Should().AllSatisfy(n => n.Should().BePositive("both dispatchers took batches"));
        (await RowsAsync()).Should().AllSatisfy(m => m.Status.Should().Be(OutboxMessageStatus.Sent));
    }

    #endregion

    /// <summary>A mail server that accepts what <paramref name="onSend"/> lets through, and records it.</summary>
    private sealed class RecordingSender(Func<EmailMessage, CancellationToken, Task>? onSend = null) : IEmailSender
    {
        private int _calls;

        public RecordingSender(Func<EmailMessage, Task> onSend) : this((m, _) => onSend(m)) { }

        public ConcurrentQueue<EmailMessage> Accepted { get; } = new();
        public int Calls => _calls;

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            if (onSend is not null)
                await onSend(message, cancellationToken);
            Accepted.Enqueue(message);
        }
    }
}
