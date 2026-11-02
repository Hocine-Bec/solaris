# Measurements

The four problems Solaris measures. For each one, the naive version is built first and tagged in git, measured,
fixed, and measured again with the same script on the same machine. No number goes into the README until its
before and after are recorded here, with the method that produced them.

**Status:** no measurement has been run yet. The method below is fixed before any result exists.

| # | Problem | Before | After | Target ([non-functional requirements](non-functional-requirements.md)) |
|---|---|---|---|---|
| **P1** | The lead request waits for email | Emails sent inside the request | Transactional outbox | p95 < 300 ms at 10 req/s |
| **P2** | Notifications get lost | The original design: a fire-and-forget Hangfire enqueue | Outbox + dispatcher with retries for 24 h | 0 lost |
| **P3** | Duplicates and lost updates under concurrency | Check-then-insert; no concurrency token | Partial unique index; `xmin` + `If-Match` | 0 duplicates, 0 lost updates |
| **P4** | Telemetry ingestion and dashboards at volume | EF Core defaults, row by row; aggregating raw readings | Composite key, set-based insert; daily rollups | ≥ 20,000 readings/s; dashboards p95 < 100 ms / 50 ms |

---

## 1. Rules

1. **Same machine, same dataset, same script** for before and after. Only the code under test changes.
2. **Record the environment** (§2) with every run. The test laptop's CPU is sometimes held near 2 GHz, so the CPU
   frequency during the run is recorded, and a clearly throttled run is redone.
3. **Warm up**, then run **3 times** and report the **median** run. All three are kept in the raw results.
4. **Report p50 / p95 / p99, the error rate and the request rate.** Never only the average.
5. **Open-model load** (a constant arrival rate), not "N users in a loop": a closed loop hides latency
   (coordinated omission).
6. **Everything is committed:** scripts in `perf/`, raw output in `perf/results/<problem>/<date>-<before|after>/`,
   with an `env.md` there.
7. **The before is a git tag** (`p1-before` … `p4-before`) on `main`, created after the naive version is merged,
   so anyone can rebuild and re-run it.
8. **The public-form rate limiter is switched off** for measurement runs (`RateLimiting__Enabled=false`);
   otherwise it would block the load. The method says so.
9. **Every generated lead has a unique email and a unique address**, except in P3a, which is about identical
   submissions. Otherwise the duplicate-lead rule (BR-L1) or the address uniqueness check changes what is
   measured.

---

## 2. Environment record

Copied into each run's `env.md`:

```
Date / time:
Git tag or commit:
Machine:        MSI Modern 15 H B13M · i7-13620H (10 cores / 16 threads) · 16 GB RAM · NVMe SSD
OS / kernel:    `lsb_release -ds` · `uname -r`
Power profile:  `powerprofilesctl get` ; CPU MHz during the run: `grep MHz /proc/cpuinfo | sort -u | head`
.NET:           `dotnet --version`  (Release build)
PostgreSQL:     postgres:17 in Docker · configuration changes (if any):
Dataset:        M / L · row counts
Load tool:      name + version · script file
Other load:     browser closed? other containers running?
Notes:
```

---

## 3. Tools

| Need | Tool | Note |
|---|---|---|
| HTTP load and fault scripts | **k6** | Readable JavaScript scripts, a constant-arrival-rate executor, built-in percentiles |
| | *or* a small Go load generator in `perf/loadgen/` | Open-model pacing and percentiles must then be implemented correctly and documented |
| Volume data | The dataset generator (§5) | `COPY`-based; produces the naive or the final schema |
| Query plans | `psql` + `EXPLAIN (ANALYZE, BUFFERS)` | Each plan saved to `perf/results/p4/explain-*.txt` |
| Delivery counts | SQL on `email_delivery_log` and `outbox_messages` | See P2 |

**Load tool:** chosen before the first run, and recorded here.

---

## 4. Measurement harness *(planned)*

These parts exist only to make the measurements possible. They are registered in the Development environment
only, and measurement runs use the Development environment with a Release build.

- **Synchronous notifications** (`Notifications__Mode=Synchronous`): the P1 "before". The request sends its
  emails itself, before answering. The API refuses to start with this mode outside Development.
- **Email senders**, layered as decorators around the real sender interface:

  | Sender | Role |
  |---|---|
  | `RecordingEmailSender` | Sends nothing; waits a configurable time per email (`Email__SimulatedLatencyMs`, default 2 s) to stand in for a real mail server |
  | `FaultInjectingEmailSender` | When the fault is on, every send fails as "mail server unreachable". Toggled by a Development-only endpoint |
  | `DeliveryLoggingEmailSender` | The outermost decorator. After the inner sender succeeds, it writes one row to `email_delivery_log` in its own transaction. It is the **only** writer of that table |

- **The delivery log:**

  ```
  email_delivery_log   id bigserial PK, notification_key text (e.g. 'LeadWelcome:123'),
                       outbox_message_id uuid NULL, recipient, subject, sent_at
  ```

  One row per email the mail server accepted. Both the original Hangfire design and the outbox design send
  through the same decorator with the same keys, so their counts compare directly. A row survives even if the
  API crashes before it records the send as done, so a re-send shows up as a duplicate. If the log write itself
  fails after a successful send, that email is uncounted: rare, and stated with the results.

---

## 5. Dataset generator *(planned)*

`backend/tools/Solaris.DataGen` is a console app that creates Active installations (4–12 kW) and writes their readings
with `COPY` (`NpgsqlBinaryImporter`):

```
dotnet run --project backend/tools/Solaris.DataGen -- --installations 1000 --days 120 --seed 42 --schema final
```

- `--schema naive | final` writes the readings in the P4 "before" or "after" table layout.
- A fixed seed makes every dataset reproducible.
- It writes the installations' device keys to `perf/keys.csv`, for the ingestion runs (Development only).

The data is shaped to look real:
- a daylight bell curve, wider in summer
- a random daily cloud factor (0.3–1.0)
- per installation: 90% healthy (0.95–1.0 of expected), 7% degraded (0.6–0.8), 3% failing, with some
  zero-reading days

---

## 6. P1 — The lead request waits for email

**Question.** How much latency does sending email inside the request add, and how much does the outbox remove?

| | Build | Configuration |
|---|---|---|
| **Before** | tag `p1-before` | `Notifications__Mode=Synchronous` · `Email__Provider=Recording` · `Email__SimulatedLatencyMs=2000` (two emails ≈ 4 s per request) |
| Reference (optional) | tag `p2-before` | The original Hangfire design, for context |
| **After** | the outbox | `Notifications__Mode=Outbox` · the same simulated latency, now paid by the dispatcher instead of the request |

**Steps**
1. Reset the database; start the API in Release mode.
2. Warm up: 30 s at 2 req/s.
3. Run `POST /api/leads` at a **constant 10 req/s for 2 minutes**, each request with a unique email and address.
4. After only: also read the *time to delivery* from the database: the p95 of `sent_at − created_at` in
   `outbox_messages`.

**Realism check:** one before run at 1 req/s with `Email__Provider=Gmail`, to confirm that the simulated 2 s per
email is honest. Its result is recorded below.

**Results**

| Run | Req/s achieved | p50 | p95 | p99 | Errors | Time to delivery p95 |
|---|---|---|---|---|---|---|
| Before (synchronous) | | | | | | n/a |
| Reference (Hangfire) | | | | | | |
| After (outbox) | | | | | | |

---

## 7. P2 — Notifications get lost

**Question.** Of all the notifications that should be sent, how many are lost when the mail server is down, or
when the API crashes mid-burst?

| | Build |
|---|---|
| **Before** | tag `p2-before`: the original design (a `Task.Run` around the Hangfire enqueue, errors swallowed, `[AutomaticRetry(Attempts = 3)]` at 30/60/120 s), plus the measurement harness (§4) |
| **After** | the outbox + dispatcher |

`email_delivery_log.notification_key` identifies each logical notification, e.g. `LeadWelcome:123` or
`SalesNewLeadAlert:123` (§4).

**Scenarios** (each run for before and after)

| # | Scenario | How |
|---|---|---|
| A | Mail outage | Turn the mail fault **on**. Create 500 leads at 5 req/s. Keep the fault on for **10 minutes** from the start, then turn it off. Wait until nothing is pending (outbox empty, or Hangfire queue idle), at most 60 minutes. |
| B | API crash | Fault off. Create 500 leads at 20 req/s. About 10 s in, `kill -9` the API process and restart it at once. Wait until nothing is pending. |
| C | Both | A and B together: fault on, crash at about 10 s, restart, fault off at 10 minutes. |

**Counting** (after the wait)

```sql
-- expected: every lead produces 2 notifications
SELECT 2 * count(*) AS expected FROM leads;
-- delivered (unique) and duplicates
SELECT count(DISTINCT notification_key) AS delivered_unique,
       count(*) - count(DISTINCT notification_key) AS duplicates
FROM email_delivery_log;
-- lost = expected − delivered_unique
```

Also recorded: the **drain time**, from "fault off" or "restart" until the last delivery.

**Results**

| Scenario | Design | Expected | Delivered | **Lost** | Duplicates | Drain time |
|---|---|---|---|---|---|---|
| A outage | before | | | | | |
| A outage | after | | | | | |
| B crash | before | | | | | |
| B crash | after | | | | | |
| C both | before | | | | | |
| C both | after | | | | | |

Duplicates are reported as they are, whatever they are.

---

## 8. P3 — Duplicates and lost updates under concurrency

### P3a — Duplicate leads

**Question.** When the same form is submitted many times at the same moment (double-clicks, browser retries, a
flaky network), how many leads, notifications and errors result?

| | Build |
|---|---|
| **Before** | tag `p3-before`: the duplicate rule checked in the service (`if exists … else insert`), with **no** partial unique index |
| **After** | the partial unique index on open leads; a conflict returns the existing lead |

**Steps:** 20 rounds. Each round uses a fresh email and address, and fires **50 identical `POST /api/leads` at
once** (released together). Rate limiter off. The 50 submissions share one address, so in the before version
address creation must not fail first, or it would hide the lead race being measured.

**Per round, counted:** leads created for that email (target 1), notifications created (target 2), and
responses by status code (after, expected: 1 × 201 + 49 × 200, and 0 × 5xx).

| Design | Rounds | Rounds with > 1 lead | Max leads in one round | Extra notifications | 5xx responses |
|---|---|---|---|---|---|
| Before | 20 | | | | |
| After | 20 | | | | |

### P3b — Lost updates on installations

**Question.** When two people change the same installation at the same moment, is an update ever silently lost?

| | Build |
|---|---|
| **Before** | tag `p3-before`: no concurrency token; each change reads the installation, modifies it and saves it |
| **After** | `xmin` + `If-Match` |

**Setup:** 200 installations at **Design**, with their design data recorded, so both moves below are valid.

**Steps:** for each installation, send **at the same moment** A = a transition to Permits and B = a
cancellation with a reason. Before: no `If-Match`. After: both send the `ETag` they read first.

**Counted:**
- **lost updates:** installations where *both* requests got a 2xx
- **invalid histories:** installations with more than one history row whose `FromStatus` is Design
- **inconsistent:** the final status differs from the last history row's `ToStatus`
- **409 responses**

| Design | Installations | Lost updates | Invalid histories | Inconsistent | 409 responses |
|---|---|---|---|---|---|
| Before | 200 | | | | |
| After | 200 | | | | |

The after target is 0 / 0 / 0, with exactly one success per installation.

---

## 9. P4 — Telemetry at volume

### P4a — Backfill-storm ingestion

**Question.** After an internet outage, 1,000 devices upload two days of readings at once. How fast does
ingestion go, and do retried batches create duplicates?

| | Build |
|---|---|
| **Before** | tag `p4-before`: EF Core defaults: an `EnergyReading` entity with a `long Id` key, only the default index on `InstallationId`, a `RecordedAt` timestamp and a `decimal EnergyKwh`; for each reading, an `AnyAsync` duplicate check, then `Add`, then `SaveChanges`. Device-key authentication is already in place, and isn't what is measured |
| **After** | the composite primary key, the set-based `unnest` + `ON CONFLICT DO NOTHING` insert, and the dirty-day marks in the same transaction |

**Setup:** the generator creates 1,000 Active installations and their keys (§5). Readings: 2 days per
installation → **192,000 readings in 2,000 batches of 96**.

**Steps:** 50 concurrent devices upload all the batches as fast as allowed. **20% of the batches are sent
twice**, some at the same time as the original (simulated retries).

**Counted:**
- accepted readings ÷ wall time = **readings/s**
- p95 batch latency
- duplicates in the database:
  ```sql
  SELECT count(*) FROM (SELECT 1 FROM energy_readings GROUP BY installation_id, slot_start HAVING count(*) > 1) d;
  ```
  (for the before table: grouped by its installation and timestamp columns)
- errors

| Design | Readings | Wall time | **Readings/s** | Batch p95 | Duplicates in DB | Errors |
|---|---|---|---|---|---|---|
| Before | 192,000 | | | | | |
| After | 192,000 | | | | | |

### P4b — Dashboards at 11.5 million readings

**Question.** How fast are the three production views when the readings table holds about 11.5 million rows?

| | Build |
|---|---|
| **Before** | tag `p4-before`: the three endpoints aggregate raw readings on every request (LINQ `GroupBy` on the before table) |
| **After** | the endpoints read the daily rollups (`EnergyProduction`) |

**Setup:**
1. Dataset **M**: 1,000 installations × 120 days ≈ 11.5 million readings, generated with `--schema naive` for
   before and `--schema final` for after, with the same seed.
2. After only: run `RollupJob` until `production_dirty_days` is empty, and record **how long the full rollup
   took**.

**Steps:** for each endpoint, warm up for 30 s, then a constant 10 req/s for 1 minute, with random installations
and months or years. One representative call per endpoint is saved with `EXPLAIN (ANALYZE, BUFFERS)`, before and
after.

| Endpoint | Design | p50 | p95 | p99 | Errors | Plan file |
|---|---|---|---|---|---|---|
| Fleet (FR-P2) | before | | | | | |
| Fleet (FR-P2) | after | | | | | |
| Month (FR-P3) | before | | | | | |
| Month (FR-P3) | after | | | | | |
| Year (FR-P4) | before | | | | | |
| Year (FR-P4) | after | | | | | |
