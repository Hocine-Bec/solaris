# 18. Days counted in each installation's own time zone

**Status:** Accepted · **Last changed:** 2026-11-27

**Context:** Solaris installs across the United States. A day's production belongs to the day as lived at the
property: a UTC day moves a US afternoon onto the next date, and several states span two time zones (Texas is
Central, but El Paso is Mountain), so the state alone can't decide it.

## Options
- **A.** Every installation stores an IANA time zone, proposed from its state, confirmed by an Admin until the
  installation is Active, then fixed (BR-P4). Readings stay in UTC and are counted per local day in that zone.
- **B.** One company time zone for every installation.
- **C.** UTC days.
- **D.** The zone derived from the ZIP code, with a ZIP-to-zone table.

## Decision
**A.** `UsTimeZones` lists the eight zones an installation can be in (one name for each set of today's rules) and
the one proposed for each state. The zone is set at conversion, can be changed until switch-on, and is fixed after,
like the design: changing it later would move readings between days already added up.

Everything that counts days uses that zone: the ingestion marks a reading's local day (ADR 16), `RollupJob` sums a
day from its own local midnight to the next (ADR 17), so a day has 92, 96 or 100 slots where the clocks change and
always 96 where they don't. The fleet view shows the last day that has ended in every zone the fleet stands in
(FR-P2), so no day still running out west is shown. The conversion is done in PostgreSQL (`AT TIME ZONE`) for the
SQL, and by `LocalDays` in the Domain for the rest.

## Trade-offs
A wrong zone confirmed at switch-on stays wrong for the installation's life. Every query on days needs the
installation's zone, so readings are never grouped by a plain date. Day boundaries depend on the time-zone data of
the database and the .NET runtime: a change to US daylight-saving rules needs both updated together. B and C are
simpler but give the wrong day for most of the country; D removes the Admin's confirmation, at the cost of a table
to download and keep current.

## Alternative that could also work
**D**, if the confirmation proves to be a source of mistakes: the ZIP code is already on the address, and the table
would only change the proposal, not anything after it.
