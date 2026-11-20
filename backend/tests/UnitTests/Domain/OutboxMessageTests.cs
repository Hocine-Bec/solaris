using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace UnitTests.Domain;

// BR-N2: a notification is retried on a growing schedule, then marked Failed once 24 hours have passed.
public class OutboxMessageTests
{
    private static readonly DateTime Created = new(2026, 11, 15, 9, 0, 0, DateTimeKind.Utc);

    private static OutboxMessage NewMessage() =>
        OutboxMessage.ForLead(new Lead { Id = 128 }, OutboxMessageType.LeadWelcome, Created);

    [Fact]
    public void ForLead_IsPendingDueNow_WithATimeOrderedId()
    {
        var message = NewMessage();
        var later = OutboxMessage.ForLead(new Lead { Id = 128 }, OutboxMessageType.SalesNewLeadAlert, Created.AddMilliseconds(5));

        (message.Status, message.Attempts, message.NextAttemptAt, message.NotificationKey)
            .Should().Be((OutboxMessageStatus.Pending, 0, Created, "LeadWelcome:128"));
        message.GiveUpAt.Should().Be(Created.AddHours(24));
        message.Id.Version.Should().Be(7);
        later.Id.CompareTo(message.Id).Should().BePositive();
    }

    [Fact]
    public void BR_N2_EachFailure_WaitsTheNextDelayOfTheSchedule()
    {
        // Arrange - every attempt fails as soon as it is due
        var message = NewMessage();
        var waits = new List<TimeSpan>();

        // Act
        for (var i = 0; i < 11; i++)
        {
            var now = message.NextAttemptAt;
            message.RecordFailure("The mail server is down.", now);
            waits.Add(message.NextAttemptAt - now);
        }

        // Assert - 10 s, 30 s, 1, 2, 5, 10 and 30 min, 1, 2, 4 and 8 h: the next attempt is 15 h 48 min 40 s in
        waits.Should().Equal(
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1),
            TimeSpan.FromHours(2), TimeSpan.FromHours(4), TimeSpan.FromHours(8));
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.Attempts.Should().Be(11);
        message.NextAttemptAt.Should().Be(Created + new TimeSpan(15, 48, 40));
    }

    [Fact]
    public void BR_N2_EveryAttemptFails_TheLastIsAtTheEndOfTheDay_ThenItIsFailed()
    {
        // Arrange - twelve failures as soon as due: the twelfth waits its full 8 h, to 23 h 48 min 40 s in
        var message = NewMessage();
        for (var i = 0; i < 12; i++)
            message.RecordFailure("Down.", message.NextAttemptAt);
        message.NextAttemptAt.Should().Be(Created + new TimeSpan(23, 48, 40));

        // Act - the thirteenth's 8 h would reach past the 24 hours; the fourteenth comes at their end
        message.RecordFailure("Down.", message.NextAttemptAt);
        var last = message.NextAttemptAt;
        message.RecordFailure("Down.", last);

        // Assert
        last.Should().Be(Created.AddHours(24));
        (message.Status, message.Attempts).Should().Be((OutboxMessageStatus.Failed, 14));
    }

    [Fact]
    public void BR_N2_FailureOnceTheDayHasPassed_MarksItFailed()
    {
        var message = NewMessage();

        message.RecordFailure("The mail server is down.", Created.AddHours(24));

        message.Status.Should().Be(OutboxMessageStatus.Failed);
        (message.Attempts, message.LastAttemptAt, message.LastError)
            .Should().Be((1, Created.AddHours(24), "The mail server is down."));
    }

    [Fact]
    public void BR_N2_FailureJustBeforeTheEndOfTheDay_IsTriedOnceMoreAtTheEnd()
    {
        var message = NewMessage();

        message.RecordFailure("Down.", Created.AddHours(24).AddSeconds(-1));

        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.NextAttemptAt.Should().Be(Created.AddHours(24));
    }

    [Fact]
    public void MarkSent_RecordsTheAttemptAndTheTime()
    {
        var message = NewMessage();
        message.RecordFailure("Down.", Created);

        message.MarkSent(Created.AddSeconds(10));

        (message.Status, message.Attempts, message.SentAt, message.LastError)
            .Should().Be((OutboxMessageStatus.Sent, 2, Created.AddSeconds(10), "Down."));
    }

    [Fact]
    public void RecordFailure_LongError_IsCutToTheColumn()
    {
        var message = NewMessage();

        message.RecordFailure(new string('x', 5000), Created);

        message.LastError.Should().HaveLength(OutboxMessage.MaxErrorLength);
    }

    [Fact]
    public void Postpone_OnlyEverMovesItLater()
    {
        var message = NewMessage();

        message.Postpone(Created.AddSeconds(10));
        message.Postpone(Created.AddSeconds(5));

        message.NextAttemptAt.Should().Be(Created.AddSeconds(10));
        message.Attempts.Should().Be(0);
    }

    [Fact]
    public void SentOrFailed_CanNotChangeAgain()
    {
        var sent = NewMessage();
        sent.MarkSent(Created);
        var failed = NewMessage();
        failed.RecordFailure("Down.", Created.AddHours(24));

        ((Action)(() => sent.RecordFailure("Down.", Created))).Should().Throw<InvalidOperationException>();
        ((Action)(() => failed.MarkSent(Created))).Should().Throw<InvalidOperationException>();
    }
}
