# 10. A new lead's emails through a transactional outbox

**Status:** Accepted · **Last changed:** 2026-11-15

**Context:** Saving a lead and scheduling its two emails must succeed or fail together (BR-N1). The earlier design saved
the lead, then enqueued two Hangfire jobs in a fire-and-forget `Task.Run` that swallowed errors: a crash between the
two lost a notification in the P2 measurement ([measurements](../measurements.md#7-p2--notifications-get-lost)).

## Options
- **A.** A transactional outbox: the lead and two `outbox_messages` rows are saved in one EF Core save, which is one
  transaction; a dispatcher in the API sends them ([ADR 11](0011-outbox-dispatcher.md)).
- **B.** Keep Hangfire, and enqueue its jobs inside a `TransactionScope` with the save, with Hangfire.PostgreSql's
  `EnableTransactionScopeEnlistment`, and declare a retry policy that applies.
- **C.** A message broker (RabbitMQ, Kafka) next to the database.

## Decision
**A.** One ordinary save makes the lead and its notifications atomic, with no ambient transaction. B was checked
against this setup before deciding: the lead and the job were committed or rolled back together, but only because EF
Core closes its connection after the save, and Npgsql then hands Hangfire the same enlisted connection. With EF Core's
connection still open, the scope needs a two-phase commit, which PostgreSQL refuses by default ("prepared
transactions are disabled"), and the whole request fails. The outbox also owns what Hangfire would configure: the
retry schedule, Failed after 24 hours (BR-N2), and the Admin's list of failed notifications (FR-N2), a query on its
own table. Each message holds its lead's id only, and the email is rendered when it is sent, so no personal details
are copied.

## Trade-offs
More code to own and test: the dispatcher, its schedule, and the cleanup of sent messages. Delivery is at least once:
an email accepted by the mail server just before a crash is sent again, carrying the same Message-ID. The request
writes two more rows per lead.

## Alternative that could also work
**B**, with much less code, as long as nothing opens a second connection inside the scope; the failed notifications
would be Hangfire's failed jobs, read through its monitoring API. C solves a larger problem than this one, with more
infrastructure to run.
