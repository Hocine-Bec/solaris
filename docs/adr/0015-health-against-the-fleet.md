# 15. A day's health is judged against the fleet, not against the table alone

**Status:** Accepted · **Last changed:** 2026-11-23

**Context:** BR-P1 expects a day's production from the system's size and the month's average peak sun hours for its
state (`PeakSunHours`). Those are long-term averages with no weather in them. Judged against them alone, a cloudy
week turns every healthy system in the region Warning or Critical at once, and the office drives out to systems
that are working; a bright day does the opposite and hides a system that is quietly failing. Solaris installs
across the United States, so the weather that moves one system moves its neighbours with it.

## Options
- **A.** Compare each system's ratio with the fleet's ratio for the same day: the median of the ratios of every
  Active system that reported, nationwide. With too few reporting systems, judge the ratio on its own.
- **B.** Keep the fixed thresholds against expected production (BR-P2 as first written).
- **C.** Bring in weather or irradiance data per location and expect production from the day's own sun.
- **D.** As A, with a floor under the comparison, so the fleet can never make the test easier than some fixed level.

## Decision
**A**, keeping the five statuses and the cut-offs already approved (0.90, 0.80, 0.60, then Critical, with Offline
for a day with no readings), but measuring the ratio against the fleet's median for that day instead of against 1.
On an ordinary day the median sits near the table's own expectation and the result looks much as B's; on a bad
day every ratio falls together and nobody is flagged.

The median, not the mean, so a handful of dead systems doesn't drag the standard down and make real faults look
normal. Only systems that reported count: a silent system is Offline, and counting its zero would pull the middle
towards nothing. Below **10** reporting systems there is no meaningful middle — two bad systems would swing it — so
the ratio is judged on its own, which is B. A system switched on during a day is not judged on the days before it:
it wasn't there to produce anything.

## Trade-offs
Health now answers "is this system keeping pace with its neighbours", not "is it producing well". On a genuinely
dark day a system making almost nothing can read Excellent, because the fleet made almost nothing too; a dead
system still reads Offline, and one that falls behind its neighbours still reads Critical. A system's day can no
longer be worked out from its own readings: it needs the day's fleet median, so a recalculation for one
installation (BR-P3) depends on every other installation's readings for that day, and a day's health can change as
late data arrives. While the fleet is under ten reporting systems, every day falls back to B, so the early weeks of
a deployment behave as B did.

## Alternative that could also work
**D** answers the "Excellent on a dark day" objection, at the cost of another number to explain and tune; it can be
added later without changing anything else, since it only bounds the median. **C** is the honest version of the
whole idea, and the one to reach for if the fleet comparison ever proves too coarse, but it puts an external
service in the path of every dashboard for a problem the fleet's own data already answers.
