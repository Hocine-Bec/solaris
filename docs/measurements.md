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
8. **The rate limits are switched off** for measurement runs (`RateLimiting__Enabled=false`, which `perf/lib/common.sh`
   sets when it starts the API; the API accepts it in Development only); otherwise the quote form's 5 per minute
   would block the load. The method says so.
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
  emails itself, the welcome email and then the sales alert, through the same senders as the outbox's dispatcher, and
  answers only once both are sent. A failed send fails the request with a 500, after the lead is saved. The API
  refuses to start with this mode outside Development. It writes no outbox messages, so the before saves only what
  it saved. The default, `Outbox`, saves the two emails as outbox messages with the lead, for the dispatcher to send.
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
                       outbox_message_id uuid NULL   (the outbox message it delivered; null for the other designs)
  ```

  `notification_key` is deliberately not unique, so a re-send is a second row. The table exists in every
  environment, through the migrations, but is written only in Development.

  One row per email the mail server accepted. Both the original Hangfire design and the outbox design send
  through the same decorator with the same keys, so their counts compare directly. A row survives even if the
  API crashes before it records the send as done, so a re-send shows up as a duplicate. If the log write itself
  fails after a successful send, that email is uncounted: rare, and stated with the results.

---

## 5. Dataset generator

`backend/tools/Solaris.DataGen` is a console app that creates Active installations (4–12 kW), spread across states
and time zones, and writes their readings with `COPY` (`NpgsqlBinaryImporter`):

```
dotnet run --project backend/tools/Solaris.DataGen -c Release -- \
  --installations 1000 --days 120 --seed 42 --schema naive --connection "$CONNECTION"
```

- `--schema naive | final` writes the readings in the P4 "before" or "after" table layout. It checks the table's
  columns first and says which migration is missing rather than failing inside `COPY`, so asking for a layout the
  database hasn't got leaves nothing behind.
- `--connection` names the database, or `ConnectionStrings__Default` from the environment. It refuses one that
  already holds installations or readings, so a dataset is never doubled by a second run.
- The readings end with **yesterday**, the last day that has certainly finished in every zone, and go back
  `--days` from there. Each installation's days are its own local days (BR-P4), so a day is 92, 96 or 100 slots
  where the clocks change.
- **A fixed seed makes the same readings every time**, checked by generating twice and comparing the count, the
  sum and the first and last slots. The device keys are *not* from the seed: they are credentials, made by the
  cryptographic generator (ADR 14), so each run writes different ones to `--keys` (default `perf/keys.csv`), which
  the ingestion run then uploads with. The file is for a throwaway measurement database, never a real one.
- Measured on the test laptop: **about 96,000 readings a second** (200 installations × 30 days = 231,580 readings
  in 2.4 s, the fleet itself in 1.1 s), so dataset M is a couple of minutes.
- The fleet covers 51 states and **7** zones: no state is proposed the Aleutian zone, which only the far west of
  Alaska uses.

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
| **After** | the outbox | `Notifications__Mode=Outbox` · `Outbox__MaxConcurrentSends=20`, as many sends at once as the reference's 20 Hangfire workers · the same simulated latency, now paid by the dispatcher instead of the request |

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

**Running:** `perf/p1/run.sh <run number> <after|before>` does all of this on a fresh `postgres:18` container, with
`perf/p1/leads.js`, and writes `results.json`, k6's summary and `env.md` to `perf/results/p1/<date>-<label>/run<N>/`;
`before` starts the API with `Notifications__Mode=Synchronous`, `after` with the outbox and 20 sends at once. The
reference is the Hangfire design, which the code no longer has: it runs from the tag `p2-before`, with that tag's
version of the script (`<reference>`). 3 runs; the median run by p95 is reported.

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
| After (outbox) | 10 | 19.5 ms | 23.6 ms | 26.6 ms | 0 | 122,923 ms |

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

**After, as measured** (2026-11-20, commit `495754b` on the outbox branch, no uncommitted change, the median run by
p95; raw results in `perf/results/p1/2026-11-20-after/`): all three runs had 1200 requests in the measured phase, none
failed and none was dropped; p50 19.4, 19.5, 19.7 ms; p95 23.2, 23.6, 24 ms; p99 26.9, 26.6, 27.2 ms; time to delivery
p95 123,193, 122,923, 122,895 ms (p50 67,015, 66,734, 66,789 ms). The dispatcher sent 20 at once
(`Outbox__MaxConcurrentSends=20`). On mains power, with no browser open, on kernel 7.0.0-43 (`env.md`).

- **The request no longer waits for email:** a p95 of 23.6 ms against the before's 4,031 ms, under the 300 ms target.
- **Against the reference** (Hangfire, p95 20.3 ms): 3.3 ms more at the p95. The request runs more code than at the
  tag `p2-before` (the session and authorization checks, the same-origin check, the quote form's added checks, and
  the outbox's two rows in its transaction; the rate limits are off in measurement runs), so this difference isn't
  the outbox's alone.
- **Time to delivery** is the reference's (121,247 ms): both send 20 emails at once at 2 s each, 10 a second, while
  the load asks for 20, so the queue grows for the whole run, in both. Delivery speed is set by how many are sent at
  once, which the outbox makes a setting; the before's emails arrived sooner (4,020 ms) because each request sent its
  own, with no limit.

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
-- still waiting when the wait ends
--   before: Hangfire jobs not yet run or retried
SELECT count(*) AS pending_at_cutoff FROM hangfire.job
WHERE statename IN ('Enqueued', 'Scheduled', 'Awaiting', 'Processing');
--   after: outbox messages not yet sent (a Failed one isn't pending: it counts as lost)
SELECT count(*) AS pending_at_cutoff FROM outbox_messages WHERE status = 'Pending';
-- lost = expected − delivered_unique − pending_at_cutoff
```

A notification still pending at the cutoff isn't counted as lost: it gets its own column, so "not delivered yet" and
"gone" stay apart. Also recorded: the **drain time**, from "fault off" (A, C) or the restart (B) until the last
delivery.

**Running:** `perf/p2/run.sh <A|B|C> <run number> [after]` runs one scenario of the outbox (with
`Outbox__MaxConcurrentSends=20`, as many sends at once as the before's 20 Hangfire workers) end to end on a fresh
`postgres:18` container: the API's Release build in Development, a warm-up of read requests only (so the only leads
are the 500 counted), the load (`perf/p2/leads.js`), the fault and the crash, the wait, and the counts. Since the lead
list became the office's (2026-11-14), the warm-up signs in once as the run's own Admin and sends the token with each
read, and its second read is `GET /api/leads/999` (a 404) instead of the removed `GET /api/addresses/999`; the
measured requests, the quote form's `POST /api/leads`, are sent signed out as before. The "before" runs of
2026-11-08 used the earlier warm-up. The before, the Hangfire design, runs from the tag `p2-before`, with that tag's
version of the script. It writes
`results.json`, k6's summary and `env.md` to `perf/results/p2/<date>-<design>/<scenario>-run<N>/`; the API's own log
(`api.log`) stays out of git. Each scenario runs 3 times; the median run by Lost is reported, ties broken by drain
time.

**Results**

| Scenario | Design | Expected | Delivered | **Lost** | Pending at cutoff | Duplicates | Drain time |
|---|---|---|---|---|---|---|---|
| A outage | before | 1000 | 1000 | **0** | 0 | 0 | 794 s |
| A outage | after | 1000 | 1000 | **0** | 0 | 0 | 299 s |
| B crash | before | 958 | 958 | **0** | 0 | 0 | 301 s |
| B crash | after | 950 | 950 | **0** | 0 | 0 | 93 s |
| C both | before | 988 | 988 | **0** | 0 | 0 | 807 s |
| C both | after | 986 | 986 | **0** | 0 | 0 | 119 s |

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

**After, as measured** (2026-11-20, commit `495754b` on the outbox branch, no uncommitted change; raw results in
`perf/results/p2/2026-11-20-after/`). The dispatcher sent 20 at once (`Outbox__MaxConcurrentSends=20`), as many as the
before's Hangfire workers.

- All nine runs (Lost; expected; drain time): A 0, 0, 0 (1000 each; 293, 299, 302 s); B 0, 0, 0 (950, 950, 948;
  93, 92, 93 s); C 0, 0, 0 (986 each; 119, 121, 117 s). No duplicates, nothing pending at the cutoff, and no
  notification Failed, in any run. The median run: A run 2, B run 1 (runs 1 and 3 tie at 93 s; run 1 is reported),
  C run 1.
- **Nothing lost, where the before lost one** (B, run 1): a lead and its two notifications are saved in one
  transaction, so a crash leaves both or neither.
- **No duplicate after six crashes** (B and C), though delivery is at least once: an email sent just before the
  `kill -9` and not yet recorded would go twice. None did in these runs; that says they are rare here, not that
  they can't happen.
- **The outage drains sooner** (A: 299 s from the fault going off, against 794 s): the outbox's schedule, not
  Hangfire's 10 retries with growing random delays, decides when each email is tried again.
- **A crash drains sooner** (B: 93 s from the restart, against 301 s): the dispatcher's row locks went with its
  connection, so the restarted API took the messages at once, where the before's jobs waited out Hangfire's 5-minute
  invisibility timeout. What remains is the sending itself, 10 emails a second.
- Expected differs between runs because requests sent while the API was down failed (B: 25, 25, 26 of 500; C: 7 each),
  so those leads never existed and owe no notification.
- **Environment:** mains power, no browser open, kernel 7.0.0-43 (`env.md`); the before ran with a browser open, on
  kernel 7.0.0-42. The drain times follow the retry schedules and the timeout, not the machine's speed.

Duplicates are reported as they are, whatever they are.

---

## 8. P3 — Duplicates and lost updates under concurrency

**Scripts:** `perf/p3/run.sh <a|b> <run number> [before|after]`, on a fresh database and the API's Release build, as
P1's and P2's (§3). The design is whatever is checked out: the before at the tag `p3-before`, the after once the
fixes are merged. Results: `perf/results/p3/<date>-<before|after>/<a|b>-run<N>/` (`results.json`, `env.md`, k6's
summary and output, the API's log). The environment record's CPU frequency is read 0.3 s (`SAMPLE_AFTER`) after the
k6 script prints that its measured load starts, past P3b's setup. The before was run with the scripts at
`p3-before`, which read it after the load (below).

### P3a — Duplicate leads

**Question.** When the same form is submitted many times at the same moment (double-clicks, browser retries, a
flaky network), how many leads, notifications and errors result?

| | Build |
|---|---|
| **Before** | tag `p3-before`: the duplicate rule checked in the service (`if exists … else insert`), with **no** partial unique index |
| **After** | the partial unique index on open leads; a conflict returns the existing lead |

**Steps:** 20 rounds. Each round uses a fresh email and address, and fires **50 identical `POST /api/leads` at
once** (released together: one k6 `http.batch`, each copy on its own connection). Rate limiter off. The 50 submissions share one address, so in the before version
address creation must not fail first, or it would hide the lead race being measured: the address insert is
concurrency-safe (`ON CONFLICT`) in both versions, and only the duplicate-lead rule differs.

**Per round, counted:** leads created for that email (target 1), notifications created (target 2), and
responses by status code (after, expected: 1 × 201 + 49 × 200, and 0 × 5xx).

| Design | Rounds | Rounds with > 1 lead | Max leads in one round | Extra notifications | 5xx responses |
|---|---|---|---|---|---|
| Before | 20 | 20 | 50 | 1,950 | 0 |
| After | 20 | 0 | 1 | 0 | 0 |

**Before, as measured** (2026-11-21, tag `p3-before`, no uncommitted change; mains power, no browser; the dev
database's container running idle). 3 runs; the median run by extra notifications is reported (run 3). Raw results
in `perf/results/p3/2026-11-21-before/a-run1` … `a-run3`.
- Every round of every run made more than one lead; in each run, at least one round turned all 50 requests into
  leads.
- Extra notifications: 1,822, 1,960 and 1,950 (runs 1–3), from 931, 1,000 and 995 leads: each duplicate lead got its
  own welcome email and office alert.
- Answers: runs 1–3 got 931, 1,000 and 995 `201`s and 69, 0 and 5 `200`s (a request that found an earlier copy
  already saved); no `5xx`, no other status.
- The environment records' "CPU MHz during the run" is wrong for P3: the script read it just after the load
  ended (P1's and P2's read it during the load). It doesn't show the frequency during the runs.

**After, as measured** (2026-11-22, `main` at the merge of the fixes, `p3-before-7-gffad1dd`, no uncommitted
change; the same conditions as the before: mains power, balanced profile, no browser, the dev database's container
running idle, kernel 7.0.0-44). 3 runs; they counted the same values, so any is the median: run 2 is reported. Raw
results in `perf/results/p3/2026-11-22-after/a-run1` … `a-run3`.
- In every run, each round made one lead and its two emails: 20 leads and 40 notifications.
- Answers in every run: 20 `201`s and 980 `200`s; no `5xx`, no other status.
- The CPU frequency, sampled 0.3 s into the load: 2,409–3,485, 1,999–2,700 and 1,199–1,600 MHz across the cores
  (runs 1–3).

### P3b — Lost updates on installations

**Question.** When two people change the same installation at the same moment, is an update ever silently lost?

| | Build |
|---|---|
| **Before** | tag `p3-before`: no concurrency token; each change reads the installation, modifies it and saves it |
| **After** | `xmin` + `If-Match` |

**Setup:** 200 installations at **Design**, with their design data recorded, so both moves below are valid. They are
made through the API, as the office makes them: a lead, contacted, converted, a surveyor's part completed, moved to
Design, the design recorded. Each of these changes sends the `ETag` of the answer before it, when the API sends one,
since the after requires it (the before's API sends none, so none is sent back).

**Steps:** for each installation, send **at the same moment** A = a transition to Permits and B = a
cancellation with a reason (one k6 `http.batch`, two connections). Before: no `If-Match`. After: both send the `ETag`
they read first.

**Counted:**
- **lost updates:** installations where *both* requests got a 2xx
- **invalid histories:** installations with more than one history row whose `FromStatus` is Design
- **inconsistent:** the final status differs from the last history row's `ToStatus`
- **409 responses**

| Design | Installations | Lost updates | Invalid histories | Inconsistent | 409 responses |
|---|---|---|---|---|---|
| Before | 200 | 200 | 199 | 50 | 0 |
| After | 200 | 0 | 0 | 0 | 200 |

**Before, as measured** (2026-11-21, tag `p3-before`, the same conditions as P3a). 3 runs; lost updates and invalid
histories are the same in every run, so the median run is by inconsistent installations (run 3). Raw results in
`perf/results/p3/2026-11-21-before/b-run1` … `b-run3`.
- In every run, both requests got a `2xx` for all 200 installations, and no `409`, `5xx` or other status.
- Invalid histories: 199 in every run. On the remaining installation of each run, the cancellation's history row
  starts from Permits: its two requests didn't overlap, the move finishing before the cancellation read the
  installation, so nothing was overwritten there. Counted as the method defines it (both `2xx`), it is still among
  the 200 lost updates.
- Inconsistent (the status differs from the last history row): 47, 53 and 50. Final statuses: Permits 68, 47 and
  55; Cancelled 132, 153 and 145.
- The environment records' CPU frequency has the same error as P3a's.

The after target is 0 / 0 / 0, with exactly one success per installation.

**After, as measured** (2026-11-22, the same build and conditions as P3a's after). 3 runs; they counted the same
values, so any is the median: run 2 is reported. Raw results in `perf/results/p3/2026-11-22-after/b-run1` …
`b-run3`.
- In every run, exactly one of the two requests succeeded for each of the 200 installations, and the other got
  `409 installation.concurrent_change`: 200 `409`s, no `5xx`, no other status.
- No invalid history and no inconsistent installation in any run. Final statuses: Permits 129, 147 and 142;
  Cancelled 71, 53 and 58 (runs 1–3).
- Each change now reads the installation first, for its `ETag`, so each pair takes one more request: k6's measured
  loop took 5.44, 10.27 and 7.02 s (the before's: 2.43, 2.44 and 2.96 s). The requests themselves didn't slow down:
  p50 8.34, 8.68 and 8.5 ms (before: 8.31, 8.34 and 8.53 ms); run 2 had one request of 1.21 s (the others' slowest:
  175.76 and 180.11 ms). P3 counts outcomes, not times.
- The CPU frequency, sampled 0.3 s into the load: 400–3,708, 395–3,700 and 399–512 MHz across the cores (runs 1–3).
  The loop sends one request or one pair at a time, so most cores are idle at any moment. Run 3's sample shows no
  core above 512 MHz, but its requests took as long as the other runs' (above), and the counts don't depend on
  speed, so it wasn't redone.

---

## 9. P4 — Telemetry at volume

**Scripts:** `perf/p4/run.sh <a|b> <run number> [before|after]`, on a fresh database and the API's Release build, as
P1's, P2's and P3's (§3). Each run generates its own dataset with §5's generator, in the layout the design needs
(`naive` before, `final` after). Results: `perf/results/p4/<date>-<before|after>/<a|b>-run<N>/` (`results.json`,
`env.md`, `dataset.txt`, the plans, k6's summary and output, the API's log).

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
twice**, some at the same time as the original (simulated retries). The fleet is generated with `--days 0`, so every
reading in the database came from an upload; the batches are shuffled in a fixed order, so a retry usually overlaps
its original rather than following it. Each reading is 400 Wh, under what the smallest system in the fleet may
produce in a slot (BR-T1), so nothing is rejected for its size.

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
and months or years. The three run one after another, so each one's numbers are its own, and only the measured
minute is recorded. One representative call per endpoint is saved with `EXPLAIN (ANALYZE, BUFFERS)`, before and
after: the statements are the ones the endpoints send, read from the API's own EF Core log and written out with
their values. The fleet's view sends one of them per time zone its installations are in (7 for a nationwide fleet),
since each zone's day covers its own hours (BR-P4).

| Endpoint | Design | p50 | p95 | p99 | Errors | Plan file |
|---|---|---|---|---|---|---|
| Fleet (FR-P2) | before | | | | | |
| Fleet (FR-P2) | after | | | | | |
| Month (FR-P3) | before | | | | | |
| Month (FR-P3) | after | | | | | |
| Year (FR-P4) | before | | | | | |
| Year (FR-P4) | after | | | | | |
