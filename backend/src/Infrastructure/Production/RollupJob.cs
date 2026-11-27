using System.Diagnostics;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Production;

/// <summary>What one run of the rollup did: how many batches, how many days it added up, and how long it took.</summary>
public sealed record RollupResult(int Batches, int Days, TimeSpan Took);

/// <summary>
/// A Hangfire job, every minute: adds up the local days the ingestion marked (BR-P3), until none is left. Each batch
/// is one statement: it claims up to <see cref="BatchSize"/> marked days — <c>SKIP LOCKED</c>, so two runs never take
/// the same day — sums each day's readings over its own local hours (BR-P4), and writes the rows. A day is always
/// added up from all its readings, so doing it twice gives the same row, and a crash before the commit hands the
/// claimed days back.
///
/// A reading that lands while its day is being added up waits for this statement's lock on the mark, then marks the
/// day again once it commits: the next batch adds it up with that reading in it, so nothing is lost.
/// </summary>
public sealed class RollupJob(AppDbContext context, ILogger<RollupJob> logger)
{
    public const string RecurringJobId = "production-rollup";

    /// <summary>How many marked days one statement claims: 1,000.</summary>
    public int BatchSize { get; init; } = 1000;

    public async Task<RollupResult> RunAsync(CancellationToken cancellationToken)
    {
        var started = Stopwatch.StartNew();
        int batches = 0, days = 0, batch;
        do
        {
            batch = await RollUpBatchAsync(cancellationToken);
            if (batch > 0)
                batches++;
            days += batch;
        } while (batch == BatchSize && !cancellationToken.IsCancellationRequested);

        if (days > 0)
            logger.LogInformation("Added up {Days} production days in {Batches} batches ({Seconds:0.0} s)",
                days, batches, started.Elapsed.TotalSeconds);
        return new RollupResult(batches, days, started.Elapsed);
    }

    private async Task<int> RollUpBatchAsync(CancellationToken cancellationToken)
    {
        // A local day starts at its own midnight in the installation's zone, so it is 92, 96 or 100 slots long
        var rows = await context.Database.SqlQuery<int>($"""
            WITH claimed AS (
              DELETE FROM production_dirty_days
              WHERE ctid IN (SELECT ctid FROM production_dirty_days FOR UPDATE SKIP LOCKED LIMIT {BatchSize})
              RETURNING installation_id, day
            ), summed AS (
              SELECT c.installation_id, c.day,
                     coalesce(sum(r.energy_wh), 0)::int AS actual_wh, count(r.slot_start)::int AS reading_count
              FROM claimed c
              JOIN installations i ON i.id = c.installation_id
              LEFT JOIN energy_readings r
                ON r.installation_id = c.installation_id
               AND r.slot_start >= (c.day::timestamp AT TIME ZONE i.time_zone)
               AND r.slot_start < ((c.day + 1)::timestamp AT TIME ZONE i.time_zone)
              GROUP BY c.installation_id, c.day
            ), written AS (
              INSERT INTO energy_production (installation_id, day, actual_wh, reading_count, computed_at)
              SELECT installation_id, day, actual_wh, reading_count, now() FROM summed
              ON CONFLICT (installation_id, day) DO UPDATE
                SET actual_wh = excluded.actual_wh, reading_count = excluded.reading_count,
                    computed_at = excluded.computed_at
              RETURNING 1
            )
            SELECT count(*)::int AS "Value" FROM written
            """).ToListAsync(cancellationToken);
        return rows[0];
    }
}
