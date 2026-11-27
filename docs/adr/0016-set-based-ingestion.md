# 16. Readings written set-based: one statement per batch

**Status:** Accepted · **Last changed:** 2026-11-27

**Context:** A device sends up to 500 readings a request, and after an outage the whole fleet sends its backlog at
once. The first version checked and saved each reading through EF Core: one round trip per reading, and two batches
at the same moment could both pass the check. P4a measured it (`measurements.md` §9): 9,640 readings a second and
502 duplicate rows in 192,000 readings.

## Options
- **A.** One SQL statement per batch: the readings passed as arrays and expanded with `unnest`, inserted with
  `ON CONFLICT (installation_id, slot_start) DO NOTHING`, and the local days they touched marked in the same
  statement (ADR 17). `COPY` only for the data generator.
- **B.** EF Core's `AddRange` and one `SaveChanges`: batched, but no answer to a slot that is already there, so a
  duplicate fails the whole batch or needs a check first.
- **C.** A bulk library (EFCore.BulkExtensions or similar).
- **D.** `COPY` into a staging table, then `INSERT … SELECT … ON CONFLICT` from it.

## Decision
**A.** The primary key (the installation and the slot, ADR 12) decides what a duplicate is, and `ON CONFLICT DO
NOTHING` lets the database pass over one with no race, however many devices send it. `RETURNING` gives back the
slots really stored: the answer's `accepted` is their count, `duplicates` is the valid readings less that, and the
same rows mark their days, so a day is never marked for a reading that wasn't stored. The statement is written by
hand in `EnergyReadingRepo` and joins the unit of work's transaction when there is one.

The generator writes millions of rows to a database nobody else is using, with no duplicates to handle, so it uses
binary `COPY`, with a longer timeout than a command's usual 30 seconds.

P4a after: 38,127 readings a second and 0 duplicates (`measurements.md` §9).

## Trade-offs
The SQL lives outside EF Core: a renamed column is caught by the integration tests, not by the compiler. A batch
that repeats a slot within itself counts the repeat as a duplicate, which is the truth for the device. B is the
plain EF version and the one P4a measured the problem of; C adds a package and hides the SQL that this decision is
about; D is faster for very large loads but costs a temporary table per request, for batches of at most 500.

## Alternative that could also work
**D**, if devices ever sent much larger batches (tens of thousands of rows): `COPY` is the fastest way into
PostgreSQL, and the staging table keeps the same `ON CONFLICT` rule.
