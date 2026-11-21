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
- **PostgreSQL** holds all state: the application tables, the outbox and Hangfire's job storage.

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
└── perf/                      load and fault scripts for P1 and P2, and their raw results (see measurements.md)
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
claim their work with `FOR UPDATE SKIP LOCKED`, and Hangfire coordinates its own jobs. The dispatcher is built, and
`OutboxCleanupJob` runs daily in Hangfire; the rollup jobs are *(planned)*. A new lead's two emails are outbox
messages, saved with the lead and sent by the dispatcher (§6). Each holds the lead's id only and the email is
rendered when it is sent, so no personal details are copied into the outbox. The emails are rendered from the lead in `Infrastructure`: an HTML
part and a plain-text part, every value from the form HTML-encoded, and the wordmark sent inside the email. The
welcome email goes to an address nobody has confirmed, so it repeats nothing an email app could show as a link: the
note stays in the office's alert (the welcome email says it was sent to the office), and the fields it does repeat
(the names, street, city and monthly bill) are refused by validation when they hold a web or email address (`://`,
`@`, or a dot followed by two letters).

---

## 4. Data model

Today the code has `Lead`, `Address`, `Customer`, `Installation` (as a conversion creates it, §7), `User` and the
outbox's `OutboxMessage` (§6). The other entities are *(planned)*, each built with its feature.

| Entity | What it is | Key rules |
|---|---|---|
| **Lead** | A quote request | Its status and when it moved: `ContactedAt`, then `ClosedAt` (Converted, Disqualified or Lost), set by the server (BR-L2); the visitor's note, never replaced. One open lead per email: checked in the service today, on purpose (P3's before, §7); `NormalizedEmail` with a partial unique index over the open statuses (BR-L1) *(planned)*; an `xmin` concurrency token *(planned)* |
| **Address** | A property, stored once | `UniqueAddressHash` with a unique index ([ADR 5](adr/0005-address-natural-key.md)) |
| **Customer** | A converted lead | `NormalizedEmail` unique: a later conversion for the same email reuses the customer (BR-L3); its address is its first lead's property; status derived from its installations (BR-C1) *(planned)* |
| **Installation** | One project at one address | Its customer, its address and the lead it came from; its status is its stage (Survey at conversion); its time zone (an IANA name, proposed from its state, BR-P4). *(Planned:)* design data (size, panel count, inverter type), the Admin's confirmation of its time zone, an `xmin` concurrency token and the device-key columns `DeviceKeyPrefix` (unique), `DeviceKeyHash`, `DeviceKeyCreatedAt` |
| **InstallationStatusHistory** | One row per status change | From, to, who, when, note; append-only, never updated or deleted |
| **InstallationTechnician** | A technician's assignment | Role Surveyor, Installer or Inspector (an enum); completion date |
| **User** | A staff account | Name; email, unique whatever its case (`NormalizedEmail`, a unique index); password hash ([ADR 8](adr/0008-password-hashing.md)); role (Admin or Technician); active flag; last sign-in; the time its sessions are valid from, moved by a new password |
| **EnergyProduction** | The daily production rollup | One row per installation and local day, enforced by a unique index; `ReadingCount` and `ComputedAt` *(planned)* |

Tables for the mechanisms below (the outbox is built; the others are *(planned)*):

```
outbox_messages        id uuid PK (version 7, made by the app; the email's Message-ID), type text
                       (LeadWelcome | SalesNewLeadAlert), lead_id int FK → leads, status (Pending | Sent | Failed),
                       attempts int, created_at, give_up_at (the end of its 24 hours), next_attempt_at,
                       last_attempt_at, sent_at, retried_at timestamptz, retried_by_user_id int FK → users
                       (SET NULL), last_error text
                       unique (lead_id, type); indexes (next_attempt_at) WHERE status = 'Pending',
                       (last_attempt_at) WHERE status = 'Failed', (sent_at) WHERE status = 'Sent'

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
  ([ADR 3](adr/0003-result-objects-for-expected-outcomes.md)). The kinds are `NotFound` (404), `Conflict` (409),
  `Unauthorized` (401) and `Invalid` (400: a well-formed field only the service can find wrong, such as a current
  password that doesn't match, answered like validation, on that field); a new kind is added when a service first
  needs it. Controllers choose the success status (200, 201 with
  the new resource's URL, 204).
- **Error responses:** every one is a ProblemDetails (RFC 9457) with a stable `code` and a `traceId`:

  | Code | Status | When |
  |---|---|---|
  | `lead.not_found` | 404 | No lead has this ID |
  | `lead.transition_not_allowed` | 409 | A move BR-L2 doesn't allow from the lead's status now (a conversion of a lead that isn't Contacted, or one already moved by another Admin); the message names its status |
  | `lead.closed` | 409 | Correcting a lead that is no longer open (FR-L7) |
  | `lead.email_taken` | 409 | Correcting a lead's email to one another open lead has (BR-L1) |
  | `customer.not_found` | 404 | No customer has this ID |
  | `user.not_found` | 404 | No staff account has this ID |
  | `user.email_taken` | 409 | Another account has this email, whatever its case or spaces; a deactivated one keeps it |
  | `user.cannot_deactivate_self` | 409 | The Admin deactivating their own account |
  | `notification.not_found` | 404 | No outbox message has this ID |
  | `notification.not_failed` | 409 | Retrying an email that isn't Failed: already retried (by another Admin, or in another tab) or sent |
  | `user.cannot_set_own_password` | 409 | Setting your own password through the Users endpoint: your own is changed with the current one (`PUT /api/auth/password`) |
  | `auth.invalid_credentials` | 401 | A refused sign-in: the same answer for an unknown email, a wrong password or a deactivated account |
  | `auth.signed_out` | 401 | A valid token whose account was deactivated since, no longer exists, or has had a new password since the token was issued |
  | `auth.cross_site_request` | 403 | A request that changes data with the session cookie, or signs in or out, from a page on another site (§9) |
  | `validation_failed` | 400 | The request is malformed or fails validation; `errors` lists the fields. Also a wrong current password when changing your own (`errors.CurrentPassword`) |
  | `too_many_requests` | 429 | One client address used the quote form 5 times, or checked a password 10 times, in the current minute; `Retry-After` says when to try again (§9) |
  | `internal_error` | 500 | A bug. Outside Development the body has no exception details; the log line has the same trace id |
  | The status's name, such as `not_found`, `method_not_allowed`, `unauthorized` or `forbidden` | 4xx | Errors from the framework itself, such as an unknown URL, a request without a session to an endpoint that needs one, or a role the endpoint doesn't allow |

- **A request its client abandons** (a screen left before its data came; the staff app cancels those loads): the work
  stops through the request's token, and the request ends as `499 Client Closed Request`, logged as an ordinary
  request. A cancellation the client didn't cause is still an error.
- **Validation:** FluentValidation rejects malformed requests with 400 and the failing fields. The quote form's
  names, street, city and monthly bill can't hold a web or email address, as the welcome email repeats them (§3).
- **Timestamps:** every timestamp in a request must carry `Z` or an offset (`2026-11-02T10:00:00Z`,
  `2026-11-02T11:00:00+01:00`) and is converted to UTC; one without a zone is rejected with 400, because it could
  be UTC or local time. Responses are always UTC, with `Z`.
- **Pagination:** every list takes `?page=1&pageSize=20` (maximum 100) and returns
  `{ items, page, pageSize, totalCount }`. Each list has a total order, ending with the ID, so no row moves between
  pages: leads and customers newest first; staff accounts active first, then by name.
- **Cancellation:** every async path takes a `CancellationToken` *(planned)*.

---

## 6. Notifications

A transactional outbox: a notification is stored in the same transaction as the event that causes it, and a
background dispatcher sends it (BR-N1, BR-N2; [ADR 10](adr/0010-transactional-outbox.md),
[ADR 11](adr/0011-outbox-dispatcher.md)).

### Write path
The event and its notifications are saved in **one save, which is one transaction**:
- **Lead created:** the lead (+ its address) and two outbox messages, `LeadWelcome` and `SalesNewLeadAlert`, due at
  once. If the save fails, neither exists.
- **Installation activated** *(planned)*: the status change, its history row, the device key and an
  `InstallationActivated` message.

The HTTP request never talks to the mail server. `Notifications:Mode` picks the path: `Outbox`, the default, or
`Synchronous`, the P1 "before" kept in the measurement harness, Development only, which writes no outbox messages and
sends both emails in the request ([measurements](measurements.md#4-measurement-harness)).

### Dispatcher
`OutboxDispatcher` is a `BackgroundService`; each batch is `OutboxProcessor.SendDueAsync`:

```
every 2 s (and again at once if the last batch was full and the mail server answered):
  BEGIN
  SELECT * FROM outbox_messages
   WHERE status = 'Pending' AND next_attempt_at <= now
   ORDER BY next_attempt_at
   LIMIT 20
   FOR UPDATE SKIP LOCKED;                         -- safe with several instances
  read the batch's leads (with their addresses)
  send up to Outbox:MaxConcurrentSends at once (5 by default), each through IEmailSender
  (10 s timeout; Message-ID header = the outbox id)
     ok      → Sent, sent_at = now
     failure → attempts + 1, last_error, next attempt after the schedule's delay
               once give_up_at has passed → Failed (shown to Admin, FR-N2)
     mail server unreachable → no more sends start; the messages not tried wait until the failed one's next attempt
  COMMIT                                           -- also when the host is stopping, so a sent email isn't sent again
```

**Backoff:** 10 s, 30 s, 1 min, 2 min, 5 min, 10 min, 30 min, 1 h, 2 h, 4 h, then 8 h. No attempt is scheduled after
`give_up_at`, 24 h after the message was saved or retried: the last one comes at that moment, and if it fails the
message is Failed. Every failure is
retried, whatever the mail server answered.

**Delivery is at least once.** If the process dies after the mail server accepted a message but before `COMMIT`,
the message is still Pending and is sent again, with the same Message-ID: a rare duplicate is accepted, a loss is
not. A send abandoned because the host is stopping counts no attempt.

### Senders
`IEmailSender` hides the mail provider, chosen by `Email:Provider`. In production it is `GmailSmtpEmailSender`
(MailKit over SMTP, with a Gmail OAuth2 token); it reports a mail server it can't reach with
`MailServerUnavailableException`. `RecordingEmailSender` sends nothing and waits a set time per email: the
Development default, refused in Production. Development wraps the provider's sender in the measurement harness
([measurements](measurements.md#4-measurement-harness)).

### Administration (FR-N2)
All `Admin`; the staff app's Failed notifications screen uses them.
- `GET /api/notifications/failed` (paginated) lists the Failed messages, newest failure first: which email, to whom
  (the person, or the office for its alert, as the email service addresses them), the lead, when it was saved and
  last tried, the attempts, the last error, and who retried it before.
- `POST /api/notifications/{id}/retry` retries one; `POST /api/notifications/failed/retry` retries every message
  Failed at that moment and answers how many. A retry is one conditional `UPDATE … WHERE status = 'Failed'`:
  Pending, due now, attempts back to 0, `give_up_at` 24 h from now (the schedule starts over), `retried_at` and
  `retried_by_user_id` recorded, `created_at` kept. Two Admins retrying one email at once: one changes it, the other
  gets `409 notification.not_failed`, as does retrying one that is Pending or Sent.
- `OutboxCleanupJob`, a Hangfire recurring job, daily at midnight UTC: deletes the Sent messages sent more than 7
  days ago, 1,000 per statement. Failed ones stay until a retry sends them; the delivery log isn't touched.

---

## 7. Leads and installations *(partly planned)*

### Lead capture (BR-L1)
- **Today, on purpose:** the service checks for an open lead with the email (compared trimmed and case-insensitive),
  then inserts. It is the "before" P3a measures: two requests sent at the same moment can both pass the check
  (`measurements.md` §8).
- *(Planned, the fix:)* the database enforces the rule, with a partial unique index on `NormalizedEmail` for the open
  statuses. On a unique violation of that index (SQLSTATE `23505`), the transaction rolls back, so no duplicate
  emails exist; the existing open lead's id is returned with **200**.
- One transaction: insert or reuse the address, insert the lead, insert its outbox messages (§6), commit. The quote
  form's answer is the lead's id only (`{ id }`, 201 or 200): it never carries an existing lead's details.
- Addresses are inserted with `ON CONFLICT (unique_address_hash) DO NOTHING`, then read by their key, in that
  transaction: never check-then-insert ([ADR 5](adr/0005-address-natural-key.md)). Two leads at one property share
  its row.

### A lead's moves (BR-L2)
`Lead.TryMoveTo` holds the allowed moves as data, and records the time: `ContactedAt`, or `ClosedAt` for Converted,
Disqualified and Lost. The lead's page gets them as `next`, so the staff app shows only what is allowed now. Converted
is reached only through the conversion. An open lead's name, email and phone can be corrected (FR-L7); the email
then gets the same naive check as the form's.

### Conversion (BR-L3)
One transaction, opened before anything is written: check that the lead is Contacted, insert the customer with
`ON CONFLICT (normalized_email) DO NOTHING` and read it (a customer that already has the email is reused, so two
conversions for one email at once make one customer), add the installation at Survey at the lead's address, in the
time zone proposed from its state (`UsTimeZones`: one of 8 US zones, the one most of the state lives in), and set
the lead to Converted, linked to the customer. *(Planned:)* the lead's `xmin` token makes a simultaneous second
conversion of one lead fail with 409.

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
  account. A valid token isn't enough on its own: every request that carries one also reads the account, in one
  query, so a deactivated or deleted account, or a session started before the account's latest new password
  (`sessions_valid_from`, compared in whole seconds), stops at its next request (`401 auth.signed_out`).
- **Staff accounts** (FR-U2, FR-U4), all `Admin`: `GET /api/users` (paginated), `GET /api/users/{id}`,
  `POST /api/users` (always a technician; the Admin gives the person their password, which is never returned),
  `PUT /api/users/{id}` (name and email; the password and sessions stay), `PUT /api/users/{id}/password` (ends that
  account's sessions), `POST /api/users/{id}/deactivate` and `/reactivate` (already in that state is a success).
  The Admin can't deactivate their own account, so the office can't lock itself out, nor set their own password
  there. Emails are unique whatever their case or spaces, decided by the unique index, also between two requests at
  the same moment. Saving an account writes only the columns that changed, so a sign-in saving its time at the
  same moment can't undo a deactivation, nor a new password (unless that sign-in is also replacing a hash made with
  weaker settings, which writes the password column).
- **Changing your own password:** `PUT /api/auth/password` with the current and the new one, for any signed-in
  account. A wrong current password is `400 validation_failed` on that field, not a 401, so the session goes on. On
  success the account's other sessions end and this one gets a new cookie.
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
  only, the API reference (`/scalar`, `/openapi/v1.json`), the measurement harness's email fault switch, and the
  Hangfire dashboard. Signed
  out, an address that doesn't exist answers `401`, like one that does.
- **Policies:** `Admin`, `Technician`, and `Staff` (either role), reading the `role` claim. The leads, the staff
  accounts and the failed notifications are `Admin` (the quote form excepted); a technician calling them gets `403 forbidden`. The staff app shows a "Not allowed"
  screen for another role's page, and the API refuses the data anyway.
- **Technician scoping** *(planned)*: a technician lists and sees only the installations they are assigned to,
  enforced in the service query.
- **Devices** *(planned)*: a separate authentication scheme reads `X-Device-Key: <prefix>.<secret>`. The prefix finds
  the installation; the secret is checked against `DeviceKeyHash` (SHA-256, constant-time comparison), which is
  adequate because the secret is 32 random bytes, not a human password. The key is shown once at activation and
  removed on deactivation.
- **Rate limiting** (BR-S1): ASP.NET Core's rate limiter, a fixed window of one minute per client address: 5 on the
  quote form (`POST /api/leads`), and 10 password checks, one budget shared by signing in and changing your own
  password (a session left open can't be used to guess the current one faster than sign-in allows). Every request
  counts, whatever its answer; one refused before (no session, another site) doesn't, as the limiter runs after
  authorization. Beyond the limit: `429 too_many_requests` with `Retry-After: 60` (the window's length, by which time
  a new one has started). An IPv4 client reaching a dual-stack socket counts as its IPv4 address. The counts live in
  the API's memory, which suits a single instance. `RateLimiting__Enabled=false` switches the limits off, in
  Development only, for the measurement runs.
- **The client's address behind a proxy:** the forwarded-headers middleware believes `X-Forwarded-For` (the last
  address, the one the proxy added) and `X-Forwarded-Proto` only from the networks in `ForwardedHeaders__KnownNetworks`.
  None are listed by default, and then the middleware isn't added at all (with empty lists it would believe every
  sender), so a client can't claim another address to get past the limits. The deployment lists its proxy once its
  addresses are confirmed (Railway's edge documents `X-Real-IP` and publishes no address range).
- **Hangfire dashboard:** mapped in Development only, at `/hangfire`, as an endpoint (so the guard test sees it),
  open to anyone there like the API reference, and answering requests from the same machine only (Hangfire's own
  check). Elsewhere the address doesn't exist.
- **CORS:** only the quote form (`POST /api/leads`) answers other sites, and only the origins in
  `Cors__AllowedOrigins` (the marketing site; `http://localhost:5173` in Development): a JSON `POST`, without
  cookies. There is no default policy, so every other endpoint answers no other site. The staff app needs none: it
  calls the API on its own site ([ADR 7](adr/0007-staff-sessions-in-a-cookie.md)). An origin that isn't exactly
  scheme, host and port (a path, a trailing slash, `*`) stops the API at startup.

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
| [10](adr/0010-transactional-outbox.md) | A new lead's emails through a transactional outbox |
| [11](adr/0011-outbox-dispatcher.md) | The outbox dispatcher: a background service with `FOR UPDATE SKIP LOCKED` |

Decisions for parts not built yet are added as ADRs when those parts are built.
