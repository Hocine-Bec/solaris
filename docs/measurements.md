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
Power profile:  `powerprofilesctl get` ; on mains power or on battery ; CPU MHz during the run: `grep MHz /proc/cpuinfo | sort -u | head`
.NET:           `dotnet --version`  (Release build)
PostgreSQL:     postgres:18 in Docker · `SELECT version()` · configuration changes (if any):
Dataset:        M / L · row counts
Load tool:      name + version · script file
Other load:     browser closed? other containers running?
Notes:
```

---

## 3. Tools

| Need | Tool | Note |
|---|---|---|
| HTTP load and fault scripts | **k6** v2.3.0 | Readable JavaScript scripts, a constant-arrival-rate executor, built-in percentiles |
| Volume data | The dataset generator (§5) | `COPY`-based; produces the naive or the final schema |
| Query plans | `psql` + `EXPLAIN (ANALYZE, BUFFERS)` | Each plan saved to `perf/results/p4/explain-*.txt` |
| Delivery counts | SQL on `email_delivery_log` and `outbox_messages` | See P2 |

**Load tool: k6 v2.3.0** (chosen 2026-11-07). It has what P1–P4 need built in: the `constant-arrival-rate` executor
for the open model (rule 5), which reports dropped iterations when it can't keep the rate instead of silently
slowing down; percentiles computed from every sample; `http.batch` to release up to 50 requests at the same moment
(P3); and scenarios with start times for the fault toggles (P2). The alternative, a small Go load generator, would
have needed its own pacing, percentile and scenario code, itself a possible source of measurement errors.

- **Install:** the `k6-v2.3.0-linux-amd64.tar.gz` release from github.com/grafana/k6, checked against the
  release's `k6-v2.3.0-checksums.txt` (SHA-256), with its `k6` binary on the `PATH`. `k6 version` prints the version
  for `env.md`.
- **Expected statuses:** k6 counts every 4xx as a failed request. Each script declares the statuses its scenario
  expects (`http.setResponseCallback(http.expectedStatuses(...))`, for example 409 in P3), so its error rate counts
  only real errors.

---

## 4. Measurement harness

These parts exist only to make the measurements possible. They are registered in the Development environment
only, and measurement runs use the Development environment with a Release build.

- **Synchronous notifications** (`Notifications__Mode=Synchronous`): the P1 "before". The lead request sends its two
  emails itself, the welcome email and then the sales alert, through the same senders as the Hangfire jobs, and
  answers only once both are sent. A failed send fails the request with a 500, after the lead is saved. The API
  refuses to start with this mode outside Development. The default, `Hangfire`, enqueues the two jobs as before.
- **Email senders:** the provider's sender, chosen by `Email__Provider`, wrapped in two decorators:

  | Sender | Role |
  |---|---|
  | `RecordingEmailSender` | The provider in Development (`Email__Provider=Recording`): sends nothing; waits a configurable time per email (`Email__SimulatedLatencyMs`, default 2 s) to stand in for a real mail server. `Email__Provider=Gmail` sends real email instead (the P1 realism check) |
  | `FaultInjectingEmailSender` | When the fault is on, every send fails with `MailServerUnavailableException`, as if the mail server were unreachable, without reaching it. Off at startup |
  | `DeliveryLoggingEmailSender` | The outermost decorator. After the inner sender succeeds, it writes one row to `email_delivery_log` in its own transaction. It is the **only** writer of that table |

- **The fault toggle** (Development only): `GET /api/dev/email-fault` answers `{ "on": false }`;
  `PUT /api/dev/email-fault` with `{ "on": true }` or `{ "on": false }` turns it on or off (204), and the API logs a
  warning each time. `FaultInjection__Enabled=true` starts the API with the fault on, so an API restarted during a
  measurement (P2 scenario C) is still cut off from the mail server.

- **The delivery log:**

  ```
  email_delivery_log   id bigint identity PK, notification_key text (e.g. 'LeadWelcome:123'),
                       recipient text, subject text, sent_at timestamptz
                       outbox_message_id uuid NULL   (planned: added with the outbox)
  ```

  `notification_key` is deliberately not unique, so a re-send is a second row. The table exists in every
  environment, through the migrations, but is written only in Development.

  One row per email the mail server accepted. Both the original Hangfire design and the outbox design send
  through the same decorator with the same keys, so their counts compare directly. A row survives even if the
  API crashes before it records the send as done, so a re-send shows up as a duplicate. If the log write itself
  fails after a successful send, that email is uncounted: rare, and stated with the results.

---

## 5. Dataset generator *(planned)*

`backend/tools/Solaris.DataGen` is a console app that creates Active installations (4–12 kW), spread across states
and time zones, and writes their readings with `COPY` (`NpgsqlBinaryImporter`):

```
dotnet run --project backend/tools/Solaris.DataGen -- --installations 1000 --days 120 --seed 42 --schema final
```

- `--schema naive | final` writes the readings in the P4 "before" or "after" table layout.
- A fixed seed makes every dataset reproducible.
- It writes the installations' device keys to `perf/keys.csv`, for the ingestion runs (Development only).

The data is shaped to look real:
- a daylight bell curve around each installation's local noon, wider in summer
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

The before is the original design: until `5128768` (2025-11-15) moved the emails to Hangfire jobs, `LeadService`
awaited both of them in the request (`7b98c80`, 2025-10-27). The synchronous mode rebuilds that on today's code, so
only where the emails are sent differs; the old commits no longer build and have no delivery log. The reference is
the Hangfire design that replaced it, and the outbox is not expected to beat it on request latency: its gain is
reliability (P2).

**Steps**
1. Reset the database; start the API in Release mode.
2. Warm up: 30 s at 2 req/s.
3. Run `POST /api/leads` at a **constant 10 req/s for 2 minutes**, each request with a unique email and address.
4. Once every notification has gone, also read the *time to delivery* from the database: the p95, over the measured
   phase's leads, of an email's `sent_at` in `email_delivery_log` minus its lead's `created_at`. The same table for
   every design, written by the same code (§4).

**Running:** `perf/p1/run.sh <run number> <reference|before|after>` does all of this on a fresh `postgres:18`
container, with `perf/p1/leads.js`, and writes `results.json`, k6's summary and `env.md` to
`perf/results/p1/<date>-<label>/run<N>/`; `before` starts the API with `Notifications__Mode=Synchronous`. 3 runs; the
median run by p95 is reported.

**Realism check:** is the simulated 2 s per email honest? The before again, with `Email__Provider=Gmail`: real emails
from one Gmail account, the welcome emails to plus-addresses of it, so they all reach one inbox.
`perf/p1/run.sh 1 realism` sends 5 s, then 30 s, at 1 req/s; `perf/p1/run.sh 1 realism-sequential` sends 1 lead, then
10 more one at a time, each request starting when the previous one has answered (a closed model, unlike rule 5, so
that no two emails are ever sent at once). Both read the Gmail settings from the root `.env` and print none of them.
One run each, since they send real email. The results are recorded below.

**Results**

| Run | Req/s achieved | p50 | p95 | p99 | Errors | Time to delivery p95 |
|---|---|---|---|---|---|---|
| Before (synchronous) | 10 | 4,025.3 ms | 4,031 ms | 4,046.5 ms | 0 | 4,020 ms |
| Reference (Hangfire) | 10 | 16.2 ms | 20.3 ms | 28.9 ms | 0 | 121,247 ms |
| After (outbox) | | | | | | |

**Before, as measured** (2026-11-09, tag `p1-before`, the median run by p95; raw results in
`perf/results/p1/2026-11-09-before/`): all three runs had 1200 requests in the measured phase, none failed and none
was dropped; p50 4,026, 4,026.1, 4,025.3 ms; p95 4,031.4, 4,030.7, 4,031 ms; p99 4,046.7, 4,042.8, 4,046.5 ms; time
to delivery p95 4,020 ms in each. Each request waits for its two emails, so the simulated mail server sets its
latency: the p50 is 25.3 ms above the 4,000 ms the two sends wait. The emails, though, arrive sooner than with the
reference: within the request, a time to delivery p95 of 4,020 ms against 121,247 ms, because the reference's queue
falls behind at this load. On mains power, with no browser open, on kernel 7.0.0-43 (`env.md`).

**Reference, as measured** (2026-11-08, tag `p2-before`, the median run by p95; raw results in
`perf/results/p1/2026-11-08-reference-rerun/`): all three runs had 1200 requests in the measured phase, none failed
and none was dropped; p50 16.2, 16.4, 16.3 ms; p95 20.3, 20.5, 20.2 ms; p99 28.9, 29, 27.4 ms; time to delivery p95
121,247, 121,321, 121,325 ms. The request only saves the lead and hands its emails to Hangfire, so it stays fast; the
emails wait instead. Hangfire runs 20 workers and each email takes 2 s, so they send at most 10 emails a second while
the load asks for 20 (2 per lead at 10 leads a second), and the queue grows for the whole run.

- **A rerun.** These runs had no browser open, ran on mains power and on kernel 7.0.0-43, the conditions of the before
  and after runs. (The power source was noted after the runs: their `env.md` predates that field.) The first set,
  kept in `perf/results/p1/2026-11-08-reference/`, ran earlier the same day with a browser open, on kernel 7.0.0-42: p50 14.1, 15.1, 15.3 ms; p95 19.1, 19.8, 20.4 ms; p99 24.9, 26, 28.9 ms; time to delivery
  p95 121,468, 120,744, 120,674 ms. Its latencies are slightly lower, not higher; three runs per set can't say whether
  the kernel, the browser or chance explains the difference.
- `env.md` says `p2-before-dirty` for the rerun: the working tree had an uncommitted change to this document only, so
  the API's code was the tag's.
- The CPU frequency in `env.md` is one sample, taken 90 s in, when the machine was nearly idle: it shows how fast the
  cores ran at that moment, not whether the CPU could reach full speed.

**Realism check, as measured** (2026-11-10, the code of tag `p1-before`; `env.md` says `dirty` because these two
script options weren't committed yet; raw results in `perf/results/p1/2026-11-10-realism/` and
`perf/results/p1/2026-11-10-realism-sequential/`). Gmail's SMTP server, each email on its own connection, as
`GmailSmtpEmailSender` sends:

| Run | p50 | p95 | p99 | Failed requests | Emails accepted |
|---|---|---|---|---|---|
| One at a time (10 leads) | 4,193.5 ms | 5,154.9 ms | 5,456.9 ms | 0 | 20 of 20 |
| 1 req/s (30 leads) | 10,301 ms | 14,607.9 ms | 16,101.3 ms | 7 | 48 of 60 |
| Before, simulated 2 s per email (for comparison) | 4,025.3 ms | 4,031 ms | 4,046.5 ms | 0 | 2,400 of 2,400 |

- **One send at a time, the simulation is close:** a request with two real emails took 4,193.5 ms at the p50,
  against 4,025.3 ms simulated; real sends vary more (p95 5,154.9 ms against 4,031 ms).
- **At 1 req/s, it is optimistic.** One request a second, each taking about 10 s, kept about ten sending at once,
  and Gmail answered 7 of them "4.3.0 Temporary System Problem. Try again later" after receiving the message (5
  welcome emails, 2 sales alerts): those requests failed with a 500, and the logged emails match (48 of 60). The simulation has no such
  limit, so the before measured with it is a lower bound on the real design's latency under load, and the real design
  would also fail requests.
- A browser was open during both runs. These differences are seconds; with and without a browser, the reference's
  p95 differed by at most 1.4 ms.

---

## 7. P2 — Notifications get lost

**Question.** Of all the notifications that should be sent, how many are lost when the mail server is down, or
when the API crashes mid-burst?

| | Build |
|---|---|
| **Before** | tag `p2-before`: the original design as it really runs: a `Task.Run` around the Hangfire enqueue, errors swallowed, and Hangfire's default retry policy (10 attempts, with growing random delays), plus the measurement harness (§4) |
| **After** | the outbox + dispatcher |

The jobs declare `[AutomaticRetry(Attempts = 3)]` at 30/60/120 s, but it has never applied: they are enqueued through their interface (`Enqueue<IEmailQueueHandler>`), and Hangfire reads filter attributes from the job's type, so it finds none and uses its global default. Found while building the harness (2026-11-07) and checked with Hangfire's own filter provider. The before is measured as it runs, not as it was declared.

`email_delivery_log.notification_key` identifies each logical notification, e.g. `LeadWelcome:123` or
`SalesNewLeadAlert:123` (§4).

**Scenarios** (each run for before and after)

| # | Scenario | How |
|---|---|---|
| A | Mail outage | Turn the mail fault **on**. Create 500 leads at 5 req/s. Keep the fault on for **10 minutes** from the start, then turn it off. Wait until nothing is pending (outbox empty, or Hangfire queue idle), at most 60 minutes. |
| B | API crash | Fault off. Create 500 leads at 20 req/s. About 10 s in, `kill -9` the API process and restart it at once. Wait until nothing is pending, at most 60 minutes after the restart. |
| C | Both | A and B together, with A's load: fault on, 500 leads at 5 req/s, crash at about 10 s, restart (the fault stays on: `FaultInjection__Enabled=true` at every start), fault off at 10 minutes. |

**Counting** (after the wait)

```sql
-- expected: every lead produces 2 notifications
SELECT 2 * count(*) AS expected FROM leads;
-- delivered (unique) and duplicates
SELECT count(DISTINCT notification_key) AS delivered_unique,
       count(*) - count(DISTINCT notification_key) AS duplicates
FROM email_delivery_log;
-- still waiting when the wait ends (the before: Hangfire jobs not yet run or retried)
SELECT count(*) AS pending_at_cutoff FROM hangfire.job
WHERE statename IN ('Enqueued', 'Scheduled', 'Awaiting', 'Processing');
-- lost = expected − delivered_unique − pending_at_cutoff
```

A notification still pending at the cutoff isn't counted as lost: it gets its own column, so "not delivered yet" and
"gone" stay apart. Also recorded: the **drain time**, from "fault off" (A, C) or the restart (B) until the last
delivery.

**Running:** `perf/p2/run.sh <A|B|C> <run number> <before|after>` runs one scenario end to end on a fresh
`postgres:18` container: the API's Release build in Development, a warm-up of read requests only (so the only leads
are the 500 counted), the load (`perf/p2/leads.js`), the fault and the crash, the wait, and the counts. Since the lead
list became the office's (2026-11-14), the warm-up signs in once as the run's own Admin and sends the token with each
read, and its second read is `GET /api/leads/999` (a 404) instead of the removed `GET /api/addresses/999`; the
measured requests, the quote form's `POST /api/leads`, are sent signed out as before. The "before" runs of
2026-11-08 used the earlier warm-up. It writes
`results.json`, k6's summary and `env.md` to `perf/results/p2/<date>-<design>/<scenario>-run<N>/`; the API's own log
(`api.log`) stays out of git. Each scenario runs 3 times; the median run by Lost is reported, ties broken by drain
time.

**Results**

| Scenario | Design | Expected | Delivered | **Lost** | Pending at cutoff | Duplicates | Drain time |
|---|---|---|---|---|---|---|---|
| A outage | before | 1000 | 1000 | **0** | 0 | 0 | 794 s |
| A outage | after | | | | | | |
| B crash | before | 958 | 958 | **0** | 0 | 0 | 301 s |
| B crash | after | | | | | | |
| C both | before | 988 | 988 | **0** | 0 | 0 | 807 s |
| C both | after | | | | | | |

**Before, as measured** (2026-11-08, tag `p2-before`, raw results in `perf/results/p2/2026-11-08-before/`):

- All nine runs (Lost; expected; drain time): A 0, 0, 0 (1000 each; 821, 794, 781 s); B **1**, 0, 0 (952, 956, 958;
  300, 300, 301 s); C 0, 0, 0 (990, 990, 988; 806, 816, 807 s). No duplicates, nothing pending at the cutoff, and no
  Hangfire job Failed, in any run.
- **The one loss** (B, run 1) is the gap between saving a lead and enqueueing its jobs. About 10 s in, the API logged
  "Queueing email jobs for lead 199" and was killed before "Successfully enqueued": lead 199 was saved, its welcome
  job had been enqueued and its email was delivered, and its sales alert was never enqueued. Nothing can retry a job
  that was never stored.
- **A mail outage loses nothing here,** because the jobs retry 10 times with Hangfire's growing default delays, which
  outlast a 10-minute outage; the declared 3 retries over 3.5 minutes never applied (the note under the first
  table). The cost is the drain: the last email of the outage went out 781–821 s after the mail server came back.
- **B's drain is the 5-minute invisibility timeout:** the jobs a worker was running at the `kill -9` stay Processing
  until Hangfire hands them to another worker, 300 s later.
- Expected differs between runs because requests sent while the API was down failed (B: 21–24 of 500; C: 5–6), so
  those leads never existed and owe no notification.
- **Environment:** a browser (Brave) was open during all the runs, on kernel 7.0.0-42; `env.md` records both. The
  drain times follow Hangfire's retry schedule and its invisibility timeout, not the machine's speed. Later runs are on
  kernel 7.0.0-43.

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
