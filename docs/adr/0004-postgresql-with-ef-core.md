# 4. PostgreSQL with EF Core, and SQL where it matters

**Status:** Accepted, recorded retroactively; the hand-written SQL rule was added on 2026-10-31 ·
**Last changed:** 2026-10-31

**Context:** The system's correctness depends on the database: duplicate-free inserts, transactions, row
locking, change detection. Most data access is ordinary CRUD, but a few paths are bulk or
performance-critical: telemetry ingestion, rollups, claiming outbox messages.

## Options
- **A.** PostgreSQL, with EF Core as the default way to reach it, and hand-written SQL for the few set-based
  paths.
- **B.** PostgreSQL with Dapper (plain SQL) everywhere.
- **C.** SQL Server with EF Core.
- **D.** A document database (MongoDB).

## Decision
**A.** PostgreSQL does the integrity work application code can't guarantee under concurrency: partial unique
indexes, `INSERT … ON CONFLICT`, `FOR UPDATE SKIP LOCKED`, the `xmin` row version, `COPY` for bulk loads,
`timestamptz`, `jsonb`. EF Core handles the domain model, change tracking and migrations, with LINQ for
everyday queries. Hand-written SQL is used only where set-based work matters, through the same connection and
transaction, so the unit of work (ADR 2) still holds. One database holds the app tables, the outbox and
Hangfire's job storage: no second store to keep consistent.

## Trade-offs
Two data-access styles to know. EF Core can hide the SQL it generates, so performance-critical queries are
checked with `EXPLAIN`. The schema and SQL are PostgreSQL-specific: database portability is deliberately
given up for these features.

## Alternative that could also work
**B.** Full control over every query and the least overhead, but more code for CRUD, no change tracking, and
migrations need a separate tool. C has equivalents for most features (`rowversion`, `READPAST`, `MERGE`), but
the project, its tests and its deployment target already run on PostgreSQL. D gives up the transactions and
constraints this project depends on.
