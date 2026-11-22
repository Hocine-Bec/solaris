# 13. Optimistic concurrency: PostgreSQL's `xmin`, sent as `ETag` and back as `If-Match`

**Status:** Accepted · **Last changed:** 2026-11-21

**Context:** BR-I4: a change made on an installation that someone else has changed since must not overwrite their
change. The first version read the installation, changed it and saved it. P3b measured it (`measurements.md` §8):
for 200 installations, a move and a cancellation sent at the same moment both succeeded, every time; the histories
of 199 of them showed both, and the final status of 47, 53 and 50 of them (runs 1–3) disagreed with their own
history. The same happens, more slowly, to an Admin acting on a page loaded long before.

## Options
- **A.** A version per row, from PostgreSQL's `xmin` system column: the API answers it as the `ETag`, every change
  must send it back as `If-Match`, and a save made on an older version is refused.
- **B.** Pessimistic locks: `SELECT … FOR UPDATE` while a change is made.
- **C.** As A, with a `version` column the application increments.
- **D.** As A, with `If-Match` optional.

## Decision
**A**, with the header required: 428 `installation.version_required` without it, 409
`installation.concurrent_change` when the version is old. `Installation.Version` maps `xmin` as EF Core's row
version, so there is no column to add or to maintain. The service compares the version sent with the one it read
and refuses before trying anything; a change that loses a race between that read and its save is refused by the
save itself, whose `UPDATE` matches only the version read. Every change saves the installation's row, even one that
only adds, removes or completes an assignment, so the version moves with everything the page shows. A technician's
My work answers the same `ETag`, and marking their part complete sends it. Any `If-Match` that isn't a quoted
version (`*`, a weak tag) is read as an old version, so it is refused too.

Leads use `xmin` too, without `If-Match`: two Admins converting one lead at once get one conversion and one 409. A
lead's page that is merely old is caught by the lead's own state instead (a move it no longer allows,
`lead.transition_not_allowed`).

## Trade-offs
`xmin` moves on every update of the row: anything that came to update installations without a person's change
(nothing does today) would refuse every page open on them. `xmin` is
PostgreSQL's: another database would need C. Every client must read before it changes (the staff app does; scripts
and tests read first). B serializes simultaneous changes, but a page loaded an hour ago still overwrites what
changed since; D lets any client that forgets the header overwrite.

## Alternative that could also work
**C**: portable, visible in the schema and in backups, and only moved by the changes the application chooses, at
the cost of a column and an increment on every save.
