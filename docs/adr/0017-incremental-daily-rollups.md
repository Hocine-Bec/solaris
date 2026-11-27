# 17. Production added up per day, incrementally, from the days that changed

**Status:** Accepted · **Last changed:** 2026-11-27

**Context:** The dashboards (FR-P2 to FR-P4) show production per local day. The first version added up the raw
readings on every request, and the fleet's view needed a pass over every Active installation's day. P4b measured it
at 11.4 million readings (`measurements.md` §9): the fleet view took 41,014 ms at p50 and could serve about 4.5
requests a second of the 10 offered. Readings also arrive late, up to 30 days after their slot (BR-T1), so a day
that was added up can change.

## Options
- **A.** One row per installation and local day in `energy_production`, kept up to date incrementally: the
  ingestion marks the days it touched in `production_dirty_days`, in the same statement as the readings (ADR 16),
  and a job adds up only the marked days.
- **B.** Add up the raw readings on every request (the P4 before).
- **C.** A materialized view, refreshed on a schedule.
- **D.** TimescaleDB's continuous aggregates.

## Decision
**A.** `RollupJob` (Hangfire, every minute) repeats one statement until no marked day is left: it claims up to
1,000 marks (`DELETE … FOR UPDATE SKIP LOCKED … RETURNING`), sums each day's readings over its own local hours
(ADR 18), and upserts the rows. A day is always added up from all its readings, so doing it twice gives the same
row; a crash before the commit hands the claimed days back; two runs never take the same day. A reading that lands
while its day is being added up waits for the lock on the mark, then marks the day again.

The rows hold only what the readings say: the energy, how many readings, when it was added up. What a day should
have made and how it went (BR-P1, BR-P2, ADR 15) are worked out when it is read, so a late reading changes one row.
No job writes "nothing" rows: a day without a row is a day the system said nothing, and reads as Offline on the
fleet view and zero on the month's and the year's.

P4b after: the fleet view 37.5 ms at p50, serving the full 10 requests a second (`measurements.md` §9).

## Trade-offs
The dashboards are up to about a minute behind the readings, plus the time to work through the marks after a large
backlog (P4b's full rollup of 118,863 days took about 22 s). There is a second copy of the data to keep right, and
its correctness rests on the mark being written with the reading, every time. B is always current but its cost
grows with the readings; C refreshes everything, so its cost grows with the data rather than with what changed; D
is an extension and a download, and not verified as available on the host planned for production.

## Alternative that could also work
**C**, while the fleet is small: one `REFRESH MATERIALIZED VIEW CONCURRENTLY` and no marks table to maintain. It
stops paying once a refresh takes longer than the dashboards can stay stale.
