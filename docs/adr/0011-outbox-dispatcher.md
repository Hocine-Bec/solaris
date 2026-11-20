# 11. The outbox dispatcher: a background service with `FOR UPDATE SKIP LOCKED`

**Status:** Accepted · **Last changed:** 2026-11-15

**Context:** The outbox ([ADR 10](0010-transactional-outbox.md)) needs something to send its messages within seconds,
retry them on BR-N2's schedule, and stay correct with several API instances. Hangfire stays for scheduled jobs.

## Options
- **A.** A `BackgroundService` in the API that every 2 s locks a batch of due messages with
  `SELECT … FOR UPDATE SKIP LOCKED`, sends them, records each outcome and commits.
- **B.** A Hangfire recurring job that does the same.
- **C.** PostgreSQL `LISTEN/NOTIFY`, waking the dispatcher when a message is written.

## Decision
**A.** A message goes out within about 2 s, and the next batch is taken at once while batches are full. Row locks
make several dispatchers safe: each skips the messages another holds, and a test runs two at once without a message
sent twice. A crash releases its locks with its connection, so its messages are taken again at the next batch. A
batch sends up to `Outbox:MaxConcurrentSends` emails at once (5 by default, below the ~10 at which Gmail refused
emails from one account); when the mail server can't be reached, no more sends start and the rest of the batch waits
with the message that failed.

## Trade-offs
Polling costs one query every 2 s when there is nothing to send. A batch holds its transaction open while it sends,
at most one 10-second timeout per wave of sends. A message sent just before a crash is sent again (at least once).

## Alternative that could also work
**C** delivers sooner than polling, but notifications can be missed while the dispatcher is down, so it would still
need polling behind it. B's recurring jobs run once a minute at most.
