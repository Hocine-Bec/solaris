using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Outbox;

/// <summary>
/// A Hangfire job, daily: deletes the outbox messages sent more than 7 days ago, 1,000 at a time so no statement holds
/// many rows. Failed messages stay until a retry sends them; the measurement harness's delivery log isn't touched.
/// </summary>
public sealed class OutboxCleanupJob(AppDbContext context, TimeProvider time, ILogger<OutboxCleanupJob> logger)
{
    public const string RecurringJobId = "outbox-cleanup";
    public static readonly TimeSpan KeepSentFor = TimeSpan.FromDays(7);

    /// <summary>How many rows one statement deletes: 1,000.</summary>
    public int BatchSize { get; init; } = 1000;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = time.GetUtcNow().UtcDateTime - KeepSentFor;
        var sent = nameof(OutboxMessageStatus.Sent);
        var deleted = 0;
        int batch;
        do
        {
            batch = await context.Database.ExecuteSqlAsync($"""
                DELETE FROM outbox_messages
                WHERE id IN (SELECT id FROM outbox_messages
                             WHERE status = {sent} AND sent_at < {cutoff}
                             LIMIT {BatchSize})
                """, cancellationToken);
            deleted += batch;
        } while (batch == BatchSize);

        logger.LogInformation("Deleted {Count} outbox messages sent before {Cutoff:O}", deleted, cutoff);
        return deleted;
    }
}
