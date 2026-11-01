# Non-functional requirements

How well Solaris must work: the scale it is designed for and the quality targets it must meet. These are
**targets, not results**: each one is checked by a measurement in [measurements](measurements.md), and no
result is claimed until it is recorded there. `[GUESS]` marks an assumption.

---

## Scale assumptions

- **Fleet:** 1,000 Active installations `[GUESS — a mid-size regional installer]`.
- **Readings:** 96 per installation per day, one per 15-minute slot including zeros at night: 96,000 a day
  across the fleet.
- **Normal rate:** about 1.1 readings per second, which is trivial.
- **Backfill storm:** after an internet outage, 1,000 devices upload two days of readings at once: 192,000
  readings within minutes. This is the throughput case that matters. `[GUESS]`
- **Measurement dataset M:** 1,000 installations × 120 days ≈ **11.5 million readings**. Dataset L (optional):
  one year ≈ 35 million.

---

## Targets

| Area | Target | Measured by |
|---|---|---|
| Lead submission | p95 < 300 ms at 10 requests/s, with notifications enabled `[GUESS]` | P1 |
| Notifications | **0 lost** after a 10-minute mail outage and after an API crash mid-burst | P2 |
| Duplicate leads | **0 duplicates** under 50 identical simultaneous submissions | P3a |
| Concurrent stage changes | **0 lost updates** and 0 invalid status histories | P3b |
| Backfill ingestion | ≥ 20,000 readings/s on a development laptop `[GUESS]`; 0 duplicates after replaying batches | P4a |
| Fleet dashboard (FR-P2) | p95 < 100 ms at dataset M `[GUESS]` | P4b |
| Month and year views (FR-P3, FR-P4) | p95 < 50 ms at dataset M `[GUESS]` | P4b |
