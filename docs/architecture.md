# Architecture

How Solaris is built: its context, its parts, how they work together at runtime, and how the key mechanisms
keep the system correct. The reasons behind each important choice are in the [ADRs](adr/) (§10).

**Status:** this describes version 1. Sections and items marked *(planned)* are not built yet; everything else
exists in the code.

---

## 1. System context

```
 Visitor ──▶ Marketing site (React) ──HTTP──▶ ┌───────────────┐ ──SMTP──▶ Gmail
 Staff ──▶ Staff app (React) ──────────HTTP──▶ │  Solaris API  │
 Inverter gateway ── HTTP, API key (planned) ─▶ └───────┬───────┘
                                                        ▼
                                                   PostgreSQL
```

- **Visitors** use the marketing site, whose quote form posts to the public lead endpoint. Its street field
  suggests addresses from Geoapify, called directly from the browser. A typed address works too; only a picked one
  sends its coordinates, so they are never guessed.
- **Staff** (Admin, Technician) sign in to the staff app with an email and a password; the session is a token in an
  HttpOnly cookie (§9). The app's frame is built (the navigation by role, the account menu, the themes, the states every
  screen shares and the API client); its screens come with their features, so until then staff use the API directly,
  through its API reference (Scalar).
- **Inverter gateways** upload production readings for their own installation *(planned)*.
- **Gmail** delivers the notification emails over SMTP.
- **PostgreSQL** holds all state: the application tables, the outbox *(planned)* and Hangfire's job storage.

---

## 2. Building blocks

```
solaris/
├── .github/workflows/ci.yml   CI: builds and tests the API and both frontends on every pull request
├── global.json                .NET SDK version and test runner
├── backend/
│   ├── Solaris.slnx
│   ├── Directory.Build.props  shared build settings (target framework, nullable)
│   ├── Directory.Packages.props  every NuGet package version, in one place
│   ├── src/
│   │   ├── Domain/            entities, enums, business rules (state machines, health, expected production)
│   │   ├── Application/       services (use cases), DTOs, validators, mappings, repository and service interfaces
│   │   ├── Infrastructure/    EF Core (AppDbContext, configurations, migrations), repositories, email, background jobs
│   │   └── WebAPI/            controllers, authentication, error handling, startup wiring
│   ├── tests/                 unit and integration tests (see testing.md)
│   └── tools/Solaris.DataGen/ dataset generator for the measurements (planned; see measurements.md)
├── frontend/
│   ├── brand/                 the tokens, the core components' CSS, the fonts, the logo and the line drawings, shared by the frontends
│   ├── marketing/             the marketing site and the quote form
│   └── staff/                 the staff app
└── perf/                      load and fault scripts, and their raw results (planned; see measurements.md)
```

Dependencies point inward: `WebAPI` → `Infrastructure` → `Application` → `Domain` ([ADR 1](adr/0001-layered-solution.md)).
`Domain` and `Application` don't reference EF Core, Npgsql, Hangfire or MailKit; an architecture test fails if
they do.

---

## 3. Runtime view

```
              ┌──────────────────────── API process (ASP.NET Core) ─────────────────────────┐
 HTTP ───────▶ Controllers ─▶ Services ─▶ Repositories / EF Core ─────────────┐             │
              │                                                               ▼             │
              │ OutboxDispatcher (every 2 s) ─▶ IEmailSender ─▶ SMTP      PostgreSQL        │
              │ Hangfire: RollupJob (every minute), CloseDayJob (01:00 local),             │
              │           OutboxCleanupJob (daily)                                          │
              └─────────────────────────────────────────────────────────────────────────────┘
```

One API process holds everything. Several instances can run side by side: the dispatcher and the rollup job
claim their work with `FOR UPDATE SKIP LOCKED`, and Hangfire coordinates its own jobs. The dispatcher and the
three jobs are *(planned)*; today, the two new-lead emails are sent by Hangfire jobs enqueued after the lead is
saved, which the outbox replaces (§6). Each job carries the lead's id only and reads the lead when it runs, so no
personal details are copied into Hangfire's tables. The emails are rendered from the lead in `Infrastructure`: an HTML
part and a plain-text part, every value from the form HTML-encoded, and the wordmark sent inside the email.

---

## 4. Data model

Today the code has `Lead`, `Address`, `Customer` and `User`. The other entities are *(planned)*, each built with its
feature.

| Entity | What it is | Key rules |
|---|---|---|
| **Lead** | A quote request | One open lead per email: `NormalizedEmail` with a partial unique index over the open statuses (BR-L1) *(planned)*; an `xmin` concurrency token *(planned)* |
| **Address** | A property, stored once | `UniqueAddressHash` with a unique index ([ADR 5](adr/0005-address-natural-key.md)) |
| **Customer** | A converted lead | Email unique; status derived from its installations (BR-C1) *(planned)* |
| **Installation** | One project at one address | Its status is its stage; design data (size, panel count, inverter type); its time zone (an IANA name, BR-P4); an `xmin` concurrency token and the device-key columns `DeviceKeyPrefix` (unique), `DeviceKeyHash`, `DeviceKeyCreatedAt` *(planned)* |
| **InstallationStatusHistory** | One row per status change | From, to, who, when, note; append-only, never updated or deleted |
| **InstallationTechnician** | A technician's assignment | Role Surveyor, Installer or Inspector (an enum); completion date |
| **User** | A staff account | Name; email, unique whatever its case (`NormalizedEmail`, a unique index); password hash ([ADR 8](adr/0008-password-hashing.md)); role (Admin or Technician); active flag; last sign-in |
| **EnergyProduction** | The daily production rollup | One row per installation and local day, enforced by a unique index; `ReadingCount` and `ComputedAt` *(planned)* |

Tables for the mechanisms below *(planned)*:

```
outbox_messages        id uuid PK, type text, payload jsonb, status (Pending | Sent | Failed),
                       attempts int, next_attempt_at timestamptz, last_error text, created_at, sent_at
                       index: (next_attempt_at) WHERE status = 'Pending'

energy_readings        installation_id int, slot_start timestamptz, energy_wh int
                       PK (installation_id, slot_start)

production_dirty_days  installation_id int, day date, PK (installation_id, day)
                       (local days whose rollup must be recalculated)
```

`energy_wh` is an integer number of watt-hours: 4 bytes instead of a `numeric`, and exact. At about 11.5 million
readings, the table and its index stay under 1 GB.

Conventions: snake_case names for tables, columns, keys and indexes (a test checks the migrated database), enums
stored as strings, every timestamp a UTC `timestamptz`. Only the daily rollups use a local day: the
installation's own time zone (BR-P4).

---

## 5. Request handling

- **One service operation is one transaction.** Controllers stay thin; a service runs the use case through the
  repositories, which add, change and query but never save; the operation ends with a single save through the
  unit of work ([ADR 2](adr/0002-services-and-repositories.md)). The unit of work turns the two failures a service
  can expect into an outcome: `Conflict` (the row changed or was deleted after it was read) and `Duplicate` (a unique
  index rejected the write).
- **Outcomes:** expected failures are `Result` values carrying an `Error` (a kind, a stable code and a message);
  unexpected errors are exceptions, handled once by a global handler
  ([ADR 3](adr/0003-result-objects-for-expected-outcomes.md)). The kinds are `NotFound` (404), `Conflict` (409) and
  `Unauthorized` (401); a new kind is added when a service first needs it. Controllers choose the success status (200, 201 with
  the new resource's URL, 204).
- **Error responses:** every one is a ProblemDetails (RFC 9457) with a stable `code` and a `traceId`:

  | Code | Status | When |
  |---|---|---|
  | `lead.not_found` | 404 | No lead has this ID |
  | `lead.already_converted` | 409 | The lead is already converted |
  | `lead.concurrent_change` | 409 | Someone else changed the lead since it was read |
  | `address.already_exists` | 409 | An address with the same key exists ([ADR 5](adr/0005-address-natural-key.md)) |
  | `customer.email_taken` | 409 | A customer already has this email |
  | `auth.invalid_credentials` | 401 | A refused sign-in: the same answer for an unknown email, a wrong password or a deactivated account |
  | `auth.signed_out` | 401 | A valid token whose account was deactivated since, or no longer exists |
  | `auth.cross_site_request` | 403 | A request that changes data with the session cookie, or signs in or out, from a page on another site (§9) |
  | `validation_failed` | 400 | The request is malformed or fails validation; `errors` lists the fields |
  | `internal_error` | 500 | A bug. Outside Development the body has no exception details; the log line has the same trace id |
  | The status's name, such as `not_found`, `method_not_allowed`, `unauthorized` or `forbidden` | 4xx | Errors from the framework itself, such as an unknown URL, a request without a session to an endpoint that needs one, or a role the endpoint doesn't allow |

- **Validation:** FluentValidation rejects malformed requests with 400 and the failing fields.
- **Timestamps:** every timestamp in a request must carry `Z` or an offset (`2026-11-02T10:00:00Z`,
  `2026-11-02T11:00:00+01:00`) and is converted to UTC; one without a zone is rejected with 400, because it could
  be UTC or local time. Responses are always UTC, with `Z`.
- **Pagination:** every list takes `?page=1&pageSize=20` (maximum 100) and returns
  `{ items, page, pageSize, totalCount }`. Each list has a total order, ending with the ID, so no row moves between
  pages: leads newest first.
- **Cancellation:** every async path takes a `CancellationToken` *(planned)*.

---

## 6. Notifications *(planned)*

A transactional outbox: a notification is stored in the same transaction as the event that causes it, and a
background dispatcher sends it (BR-N1, BR-N2).

### Write path
The event and its notifications are saved in **one save, which is one transaction**:
- **Lead created:** the lead (+ its address) and two outbox messages, `LeadWelcome` and `SalesNewLeadAlert`.
- **Installation activated:** the status change, its history row, the device key and an `InstallationActivated`
  message.

The HTTP request never talks to the mail server.

### Dispatcher
`OutboxDispatcher` is a `BackgroundService`:

```
every 2 s (and again immediately if the last batch was full):
  BEGIN
  SELECT … FROM outbox_messages
   WHERE status = 'Pending' AND next_attempt_at <= now()
   ORDER BY next_attempt_at
   LIMIT 20
   FOR UPDATE SKIP LOCKED;                         -- safe with several instances
  for each message:
     send through IEmailSender (10 s timeout; Message-ID header = the outbox id)
       ok      → status = Sent, sent_at = now()
       failure → attempts + 1, last_error, next_attempt_at = now() + backoff(attempts)
                 once created_at + 24 h has passed → status = Failed (shown to Admin, FR-N2)
     if the mail server is unreachable: stop the batch and reschedule the rest
  COMMIT
```

**Backoff:** 10 s, 30 s, 1 min, 2 min, 5 min, 10 min, 30 min, 1 h, 2 h, 4 h, 8 h; Failed once 24 hours have passed.

**Delivery is at least once.** If the process dies after the mail server accepted a message but before
`COMMIT`, the message is still Pending and is sent again: a rare duplicate is accepted, a loss is not.

### Senders
`IEmailSender` hides the mail provider, chosen by `Email:Provider`. In production it is `GmailSmtpEmailSender`
(MailKit over SMTP, with a Gmail OAuth2 token); it reports a mail server it can't reach with
`MailServerUnavailableException`. `RecordingEmailSender` sends nothing and waits a set time per email: the
Development default, refused in Production. Development wraps the provider's sender in the measurement harness
([measurements](measurements.md#4-measurement-harness)). Built today; until the outbox, the Hangfire jobs send
through it.

### Administration
- `GET /api/notifications/failed` (paginated) lists the Failed messages.
- `POST /api/notifications/{id}/retry` sets a message back to Pending, due now.
- `OutboxCleanupJob` deletes Sent messages older than 7 days, daily.

---

## 7. Leads and installations *(planned)*

### Duplicate-safe lead capture (BR-L1)
- The database enforces the rule: a partial unique index on `NormalizedEmail` for the open statuses.
- One transaction: insert or reuse the address, insert the lead, insert its outbox messages, commit.
- On a unique violation of the open-lead index (SQLSTATE `23505`), the transaction rolls back, so no duplicate
  emails exist; the existing open lead is loaded and returned with **200**.
- Addresses are inserted with `ON CONFLICT (UniqueAddressHash) DO NOTHING`, then selected by hash: never
  check-then-insert.

### Conversion (BR-L3)
One transaction: check that the lead is Contacted, create the customer (Prospect) and the installation (Survey)
at the lead's address, set the lead to Converted and link the customer. The lead's `xmin` token makes a
simultaneous second conversion fail with 409.

### The installation state machine (BR-I1–BR-I3)
- The allowed transitions and their conditions live in the `Domain` as data plus one method,
  `Installation.TransitionTo(target, actor, note)`, which returns a `Result`. It checks the transition and its
  condition (assignments, design data), appends the history row and sets the dates.
- The service recalculates the customer's status (BR-C1) in the same transaction.

### Optimistic concurrency (BR-I4)
- Installation and Lead map PostgreSQL's `xmin` system column as their concurrency token; no extra column is
  needed.
- `GET /api/installations/{id}` returns `ETag: "<xmin>"`. Every change (transition, cancellation, design,
  assignment) requires `If-Match: "<xmin>"`: 428 if the header is missing, 409 if it is stale. The 409 says:
  *"This installation was changed by someone else. Reload and try again."*

---

## 8. Telemetry and production *(planned)*

### Ingestion
`POST /api/telemetry/readings`, authenticated by the device key:

- Body: `{ "readings": [ { "slotStart": "2026-10-19T14:15:00Z", "energyWh": 1830 }, … ] }`, at most 500.
- The installation comes **from the key**, never from the body, so a device can't write for another
  installation (BR-T2).
- Each reading is validated (BR-T1); invalid ones are returned in `rejected` with a reason. Valid ones are
  written **set-based**, in one transaction:

  ```sql
  WITH inserted AS (
    INSERT INTO energy_readings (installation_id, slot_start, energy_wh)
    SELECT @installationId, s, w FROM unnest(@slots, @wh) AS t(s, w)
    ON CONFLICT (installation_id, slot_start) DO NOTHING
    RETURNING slot_start
  )
  INSERT INTO production_dirty_days (installation_id, day)
  SELECT DISTINCT @installationId, (slot_start AT TIME ZONE @timeZone)::date FROM inserted  -- the installation's zone
  ON CONFLICT DO NOTHING;
  ```

- Response: `accepted` = rows inserted; `duplicates` = valid − accepted; `rejected` = the list with reasons.

### Rollups (BR-P1–BR-P3)
- **`RollupJob`** (Hangfire, every minute), in one transaction:
  1. Claim up to 1,000 rows from `production_dirty_days`
     (`DELETE … WHERE ctid IN (SELECT ctid … FOR UPDATE SKIP LOCKED LIMIT 1000) RETURNING *`).
  2. For each installation and day: sum `energy_wh` over that local day (a primary-key range scan), compute
     expected production (BR-P1) and health (BR-P2), and upsert the `EnergyProduction` row.
  3. Commit. If the job crashes, the claimed rows come back; recalculating is idempotent.
- **`CloseDayJob`** (Hangfire, every hour): every Active installation whose local day ended at least an hour ago,
  with no rollup for that day, gets an **Offline** row (BR-P2), so a dead system doesn't simply disappear from the
  dashboards.
- **Local days** follow each installation's time zone (BR-P4), so daylight-saving days have 92 or 100 slots and
  zones without daylight saving always 96: counts use real local-day boundaries, never "96 slots".

### Dashboards
They read rollups only:

| View | Query |
|---|---|
| Fleet (FR-P2) | Count by health for the last day ended in every zone, plus the 20 lowest actual ÷ expected ratios; indexed on the date |
| Month (FR-P3) | One installation's rollups for one month (covered by the unique index) |
| Year (FR-P4) | One installation's rollups for one year, summed by month |

---

## 9. Security

Staff sign-in, sessions and the roles' checks are built; the parts marked *(planned)* aren't.

- **Staff sign-in** (FR-U1): `POST /api/auth/login` looks the account up by its normalized email and checks the
  password ([ADR 8](adr/0008-password-hashing.md)). Every refusal (an unknown email, a wrong password, a deactivated
  account) gets the same answer, `401 auth.invalid_credentials`, and takes the same time: an unknown email is checked
  against a stand-in hash. On success it records `LastLoginAt`, replaces a hash made with weaker settings, and issues
  a JWT (HS256, `Jwt:LifetimeHours`, 8 hours) with the claims `sub`, `email` and `role`.
- **The session cookie** ([ADR 7](adr/0007-staff-sessions-in-a-cookie.md)): the token travels in `solaris_session`,
  `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api`, expiring with the token. The API also reads the
  `Authorization: Bearer` header, which wins when both are sent (the API reference, tests, scripts).
  `POST /api/auth/logout` clears the cookie, even when the session had already ended. `GET /api/auth/me` returns the
  account. A valid token isn't enough on its own: every request that carries one also reads the account, so a
  deactivated or deleted account stops at its next request (`401 auth.signed_out`).
- **Requests from another site:** a request that changes data and carries the session cookie, or that signs in or
  out, is refused with `403 auth.cross_site_request` when its `Origin` isn't the API's own host. A request without
  `Origin` passes: browsers send it, scripts don't. The public lead form carries no cookie, so it isn't affected.
- **The password rule**, for every password set: at least 8 characters, with an uppercase letter, a number and a
  special character (not a letter, a number or a space), at most 128. Signing in checks only that one was given.
- **The first Admin** (FR-U3): at startup, when no active Admin exists, one is created from the `Admin` settings
  (name, email, password, checked with the other settings); with neither, the API stops and names the settings. There
  are no default credentials in the code.
- **Closed by default** ([ADR 9](adr/0009-closed-by-default.md)): the fallback authorization policy requires a
  signed-in account, so a new endpoint is protected unless it is marked `[AllowAnonymous]`. Open on purpose: the quote
  form (`POST /api/leads`), sign-in and sign-out, and the health checks when they come *(planned)*; in Development
  only, the API reference (`/scalar`, `/openapi/v1.json`) and the measurement harness's email fault switch. Signed
  out, an address that doesn't exist answers `401`, like one that does.
- **Policies:** `Admin`, `Technician`, and `Staff` (either role), reading the `role` claim. The leads are `Admin`
  (the quote form excepted); a technician calling them gets `403 forbidden`. The staff app shows a "Not allowed"
  screen for another role's page, and the API refuses the data anyway.
- **Technician scoping** *(planned)*: a technician lists and sees only the installations they are assigned to,
  enforced in the service query.
- **Devices** *(planned)*: a separate authentication scheme reads `X-Device-Key: <prefix>.<secret>`. The prefix finds
  the installation; the secret is checked against `DeviceKeyHash` (SHA-256, constant-time comparison), which is
  adequate because the secret is 32 random bytes, not a human password. The key is shown once at activation and
  removed on deactivation.
- **Rate limiting** *(planned)*: the built-in rate limiter, fixed window per client IP: 5 per minute on
  `POST /api/leads`, 10 per minute on sign-in. Behind a proxy, forwarded headers are enabled so the real client IP is
  used.
- **Hangfire dashboard** *(planned)*: mounted only in Development.
- **CORS** *(planned)*: only the configured origins (the marketing site). The staff app needs none: it calls the API
  on its own site.

---

## 10. Decisions

Architecture decisions are recorded as ADRs, one file each, in [`adr/`](adr/):

| ADR | Decision |
|---|---|
| [1](adr/0001-layered-solution.md) | Layered solution |
| [2](adr/0002-services-and-repositories.md) | Services and repositories, with one save per operation |
| [3](adr/0003-result-objects-for-expected-outcomes.md) | Result objects for expected outcomes, exceptions for the unexpected |
| [4](adr/0004-postgresql-with-ef-core.md) | PostgreSQL with EF Core, and SQL where it matters |
| [5](adr/0005-address-natural-key.md) | Addresses deduplicated by a normalized natural key |
| [6](adr/0006-staff-app-stack.md) | The staff app's stack and its test level |
| [7](adr/0007-staff-sessions-in-a-cookie.md) | Staff sessions: the token in an HttpOnly cookie |
| [8](adr/0008-password-hashing.md) | Passwords hashed with PBKDF2-HMAC-SHA512 |
| [9](adr/0009-closed-by-default.md) | Closed by default: every endpoint needs a signed-in account unless it opens itself |

Decisions for parts not built yet are added as ADRs when those parts are built.
