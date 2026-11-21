using System.Diagnostics;
using Application.Common.Exceptions;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Outbox;

/// <summary>What one batch did; the dispatcher takes the next one at once when this one was full and the mail server answered.</summary>
public sealed record OutboxBatch(int Taken, int Sent, bool MailServerUnreachable)
{
    public bool TakeMoreNow => Taken == OutboxProcessor.BatchSize && !MailServerUnreachable;
}

/// <summary>
/// Sends one batch of due outbox messages (BR-N2), in one transaction: the due messages are locked with
/// <c>FOR UPDATE SKIP LOCKED</c>, so dispatchers in several processes take different ones; up to
/// <c>Outbox:MaxConcurrentSends</c> are sent at once, each for 10 s at most; each is then marked Sent, or given its next
/// attempt, or Failed once its 24 hours have passed. When the mail server can't be reached, no more sends start, and
/// the messages not tried wait with the one that failed. If the process dies before the commit, the messages stay
/// Pending and are sent again: delivery is at least once.
/// </summary>
public sealed class OutboxProcessor(AppDbContext context, IEmailService emails, TimeProvider time,
    IOptions<OutboxOptions> options, ILogger<OutboxProcessor> logger)
{
    public const int BatchSize = 20;

    /// <summary>How long one send may take before it counts as a failed attempt: 10 s.</summary>
    public TimeSpan SendTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// <paramref name="stopping"/> is the host shutting down: no new send starts, a send under way is abandoned without
    /// counting an attempt, and what was done is still saved.
    /// </summary>
    public async Task<OutboxBatch> SendDueAsync(CancellationToken stopping)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(stopping);
        var now = Now();
        var pending = nameof(OutboxMessageStatus.Pending);
        var batch = await context.OutboxMessages
            .FromSql($"""
                SELECT * FROM outbox_messages
                WHERE status = {pending} AND next_attempt_at <= {now}
                ORDER BY next_attempt_at
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(stopping);
        if (batch.Count == 0)
        {
            await transaction.CommitAsync(stopping);
            return new OutboxBatch(0, 0, false);
        }

        var leadIds = batch.Where(m => m.LeadId is not null).Select(m => m.LeadId!.Value).Distinct().ToList();
        var leads = await context.Leads.AsNoTracking().Include(l => l.Address)
            .Where(l => leadIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, stopping);
        var installationIds = batch.Where(m => m.InstallationId is not null).Select(m => m.InstallationId!.Value).Distinct().ToList();
        var installations = installationIds.Count == 0
            ? []
            : await context.Installations.AsNoTracking().Include(i => i.Address).Include(i => i.Customer)
                .Where(i => installationIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, stopping);

        var outcomes = await SendAllAsync(batch, new Subjects(leads, installations), stopping);

        // Applied in the batch's order, once every send has ended: the entities aren't shared between sends
        var sent = 0;
        DateTime? retryAt = null;
        for (var i = 0; i < batch.Count; i++)
        {
            var message = batch[i];
            switch (outcomes[i])
            {
                case Sent s:
                    message.MarkSent(s.At);
                    sent++;
                    logger.LogInformation("Sent outbox message {MessageId} ({NotificationKey}) in {ElapsedMs}ms",
                        message.Id, message.NotificationKey, s.ElapsedMs);
                    break;
                case Failed f:
                    Fail(message, f);
                    if (f.Unreachable)
                        retryAt ??= message.Status == OutboxMessageStatus.Pending
                            ? message.NextAttemptAt
                            : f.At + OutboxMessage.RetryDelays[0];
                    break;
            }
        }
        if (retryAt is { } until)
            for (var i = 0; i < batch.Count; i++)
                if (outcomes[i] is null)
                    batch[i].Postpone(until);

        // Saved even while stopping, so a message already sent isn't sent again
        await context.SaveChangesAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return new OutboxBatch(batch.Count, sent, retryAt is not null);
    }

    /// <summary>What the batch's emails are about, read once for the batch.</summary>
    private sealed record Subjects(Dictionary<int, Lead> Leads, Dictionary<int, Installation> Installations);

    private abstract record Outcome(DateTime At);
    private sealed record Sent(DateTime At, long ElapsedMs) : Outcome(At);
    private sealed record Failed(DateTime At, string Error, Exception Exception, bool Unreachable) : Outcome(At);

    /// <summary>Each message's outcome, in the batch's order; null for one not tried, or abandoned while stopping.</summary>
    private async Task<Outcome?[]> SendAllAsync(List<OutboxMessage> batch, Subjects subjects, CancellationToken stopping)
    {
        var outcomes = new Outcome?[batch.Count];
        var unreachable = 0;
        using var slots = new SemaphoreSlim(options.Value.MaxConcurrentSends);
        var running = new List<Task>();

        async Task RunAsync(int index)
        {
            try
            {
                outcomes[index] = await AttemptAsync(batch[index], subjects, stopping);
                if (outcomes[index] is Failed { Unreachable: true })
                    Interlocked.Exchange(ref unreachable, 1);
            }
            finally
            {
                slots.Release();
            }
        }

        for (var i = 0; i < batch.Count; i++)
        {
            try
            {
                await slots.WaitAsync(stopping);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            if (Volatile.Read(ref unreachable) == 1 || stopping.IsCancellationRequested)
            {
                slots.Release();
                break;
            }
            running.Add(RunAsync(i));
        }
        await Task.WhenAll(running);
        return outcomes;
    }

    private async Task<Outcome?> AttemptAsync(OutboxMessage message, Subjects subjects, CancellationToken stopping)
    {
        var started = time.GetTimestamp();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping);
            timeout.CancelAfter(SendTimeout);
            await SendAsync(message, subjects, timeout.Token);
            return new Sent(Now(), (long)time.GetElapsedTime(started).TotalMilliseconds);
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // Abandoned, not failed: it stays due. The mail server may have accepted it, so it may go twice.
            return null;
        }
        catch (MailServerUnavailableException ex)
        {
            return new Failed(Now(), ex.Message, ex, Unreachable: true);
        }
        catch (OperationCanceledException ex)
        {
            return new Failed(Now(),
                FormattableString.Invariant($"The mail server didn't answer within {SendTimeout.TotalSeconds:0.###} s."),
                ex, Unreachable: false);
        }
        catch (Exception ex)
        {
            return new Failed(Now(), ex.Message, ex, Unreachable: false);
        }
    }

    private Task SendAsync(OutboxMessage message, Subjects subjects, CancellationToken cancellationToken)
    {
        return message.Type switch
        {
            OutboxMessageType.LeadWelcome => emails.SendLeadWelcomeAsync(LeadOf(message, subjects), message.Id, cancellationToken),
            OutboxMessageType.SalesNewLeadAlert => emails.SendNewLeadAlertAsync(LeadOf(message, subjects), message.Id, cancellationToken),
            OutboxMessageType.InstallationActivated => emails.SendInstallationActivatedAsync(
                message.InstallationId is { } id && subjects.Installations.TryGetValue(id, out var installation)
                    ? installation
                    : throw new InvalidOperationException($"Installation {message.InstallationId} no longer exists."),
                message.Id, cancellationToken),
            var other => throw new UnreachableException($"No email for the outbox message type {other}.")
        };
    }

    private static Lead LeadOf(OutboxMessage message, Subjects subjects) =>
        message.LeadId is { } id && subjects.Leads.TryGetValue(id, out var lead)
            ? lead
            : throw new InvalidOperationException($"Lead {message.LeadId} no longer exists.");

    private void Fail(OutboxMessage message, Failed failure)
    {
        message.RecordFailure(failure.Error, failure.At);
        if (message.Status == OutboxMessageStatus.Failed)
            logger.LogError(failure.Exception, "Outbox message {MessageId} ({NotificationKey}) failed for good after {Attempts} attempts in 24 hours",
                message.Id, message.NotificationKey, message.Attempts);
        else
            logger.LogWarning(failure.Exception, "Outbox message {MessageId} ({NotificationKey}) failed, attempt {Attempts}; next at {NextAttemptAt:O}",
                message.Id, message.NotificationKey, message.Attempts, message.NextAttemptAt);
    }

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
