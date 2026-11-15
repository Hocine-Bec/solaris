# Testing

What is tested, at which level, and how. Business rules (`BR-…`) are defined in
[functional requirements](functional-requirements.md).

**Status:** today there are 81 unit tests (8 for the `Lead` and `Address` entities, 2 for the `User` entity's new
password, 9 for the outbox message's delivery schedule, 1 for the list of states, 26 for the lead request's state
code, coordinates and the fields the welcome email repeats, 12 for the password rule, 3 for the sign-in request, 18
for the account requests, and the architecture test's 2 cases, one each for `Domain` and `Application`) and 356
integration tests: 299 on PostgreSQL 18 (54 for the repositories, 2 for the unit of work, 7 for `LeadService`, 1 for
the schema's names, 1 for Hangfire's storage belonging to its host, 17 for the email senders and the measurement harness, 16 for the outbox dispatcher, a retry and
the cleanup job, and 201 through the API for error responses, pagination, timestamps, the email fault toggle, a new
lead's notifications (the outbox, and the synchronous mode), the failed notifications and their retries, signing in (the session cookie, `/me`, signing out, requests from another site and
the first Admin), the staff accounts, the rate limits and the client's address behind a proxy, CORS and the jobs'
dashboard, and who may call what: every endpoint by caller, and the guard), and, needing no database, 33 for the
settings' startup validation, 9 for a new lead's two emails, 9 for the roles' policies and 6 for a request its client
abandons (ending as 499, not as an error). The marketing site has 68
unit tests of its own, and the staff app 123 (below). Everything else below is *(planned)*.

---

## Projects

| Project | Holds |
|---|---|
| `backend/tests/UnitTests` | Tests of pure logic in `Domain` and `Application`, with no database |
| `backend/tests/IntegrationTests` | Tests against a real PostgreSQL: repositories, services, concurrency, the API |
| `backend/tests/TestUtilities` | Shared helpers: test-data factories (Bogus) and the PostgreSQL fixture, which gives each test its own database |
| `frontend/marketing/src/**/*.test.ts` | The marketing site's unit tests, beside the code they test: the quote form's checks and the request it builds (25), how each answer of the API is shown (11), which address suggestions are offered and how the list narrows as the visitor types (27), each page's title and description (5) |
| `frontend/staff/src/**/*.test.ts` | The staff app's unit tests, beside the code they test: how each answer of the API becomes an error a screen acts on, from answers recorded from the API and the answers of a gateway that couldn't reach it (25), the requests the client sends and how it checks the answers (9), which failures are retried and how a 401 ends the session (13), signing in, asking who is signed in and changing your password (7), the staff accounts' requests (7), the failed notifications' requests (4) and how their rows read (3), the sign-in form's checks and messages (11), the account forms' checks, the generated password and where the API's refusals show (8), how dates are written (4), where signing in leads, the page to return to and the initials (19), the navigation by role (5), the theme choice (4), which state a load shows (4) |

---

## Levels

| Level | What | How |
|---|---|---|
| **Unit** | The lead and installation state machines (every from/to pair, allowed or rejected), stage conditions, health classification, expected production, reading validation, the outbox backoff schedule | xUnit + FluentAssertions, no database |
| **Integration** | Services and repositories against real constraints; the outbox write path; rollup recalculation | Testcontainers `postgres:18`: one container per test run; each test gets its own database, copied from a template that has the migrations applied, so tests run in parallel without sharing data |
| **Concurrency** | 50 identical leads at once → 1 lead; two conversions of one lead → 1 success + 409; conflicting stage changes → 1 success + 409s; a stale `ETag` → 409; replayed reading batches → 0 duplicates; two dispatchers never send the same message | N parallel tasks, each with its own `DbContext` or `HttpClient`, released at the same moment, then the invariants are checked |
| **API** | Every error response is a ProblemDetails with its code and a `traceId`, and a bug's 500 shows no exception details ([architecture](architecture.md#5-request-handling)); lists are paginated, with their limits; timestamps without a zone are rejected and the others stored as UTC; signing in sets the session cookie with its attributes, every refusal gets the same answer, `/me` follows the account (a deactivation stops its session), signing out clears the cookie, a hash with fewer iterations is replaced at sign-in, a change from another site with the cookie is refused while the public form isn't, and the first Admin is created from the settings, once. The staff accounts (FR-U2): the list's order and fields (never a password), a new technician who can then sign in, an email taken whatever its case or spaces (a deactivated account's included), two creations of one email at the same moment → one account, editing, a new password that ends the account's earlier sessions, deactivating (its session ends, sign-in refused) and reactivating, the Admin unable to deactivate or reset their own account, changing your own password (the other sessions end, this one gets a new cookie; a wrong current one is a 400 on its field and the session goes on), and a deactivation saved while a sign-in checks the password staying. The rate limits (BR-S1): the sixth quote from one address in a minute → 429 with its code and `Retry-After`, nothing saved; refused quotes count too; another address has its own five; the rest of the API still answers; sign-in and changing your password share ten checks, and a 429 doesn't end the session; requests refused by authorization don't count; which endpoints are limited. The client's address: `X-Forwarded-For` ignored with no proxy listed and from a connection outside the listed networks, believed from a listed proxy, and only its last address; an IPv4 client on a dual-stack connection; the switch off in Development, and the API refusing to start with it off elsewhere or with a proxy entry that isn't a network. CORS (BR-S2): a listed site's page may send the quote form (the preflight allows a JSON `POST`, no cookies) and read its answer, a 429 included; a site not listed (another host, scheme or port) gets no permission, nor does any site when none is listed; Development lists the local site; every other endpoint answers no site, even a listed one; an origin with a path, a trailing slash or `*` stops the API at startup. The jobs' dashboard: open in Development from the same machine, refused from another, absent outside Development | `WebApplicationFactory` + Testcontainers: the real API in memory, outside Development, on the test's own database; only the email queue is a fake (and, for one test, the password hasher, which deactivates the account from another connection while it checks the password); the rate-limit tests give each request a connection address from a test header, read before the API's middleware |
| **Authorization** | Every endpoint called signed out, as a technician and as the Admin, against an explicit table of who may call it: allowed callers get past authorization, the others get 401 or 403. Also: creating a lead signed out; a valid token whose account was deactivated or deleted → 401 `auth.signed_out`; the removed endpoints gone; an unknown URL → 401 signed out; the Development-only addresses open there; each policy for each role. A device key accepted only for its own installation *(planned)* | `WebApplicationFactory` + Testcontainers, an empty body where there is one; the policies through `IAuthorizationService`, without the API |
| **Guard test** | Fails if an endpoint is open to anyone without being on the explicit list of public ones (in Development, plus the API reference, the measurement switch and the jobs' dashboard), or if an endpoint is missing from the table above, so every new endpoint's access is decided on purpose | Enumerates every endpoint the running API maps through `EndpointDataSource` |
| **Architecture test** | Fails if `Domain` or `Application` reference EF Core, Npgsql, Hangfire or MailKit ([ADR 1](adr/0001-layered-solution.md)) | Reads each project's assembly references |
| **Schema naming test** | Fails if any table, column, constraint or index in the migrated database isn't snake_case (EF Core's own `__EFMigrationsHistory` table excepted) | Reads PostgreSQL's catalog in a test database |
| **Outbox** | BR-N1: a new lead and its two outbox messages are saved together, and a lead the database refuses leaves no message. BR-N2, on the message alone: the schedule's delays in order, the last attempt at the end of the 24 hours, then Failed, and a Sent or Failed message never changing again. The dispatcher's batch on PostgreSQL: due messages sent with their key and their id (the Message-ID) and marked Sent; at most 20 a batch; a refused email waiting 10 s while the others are sent; the schedule followed attempt after attempt; Failed once the day has passed, and never taken again; a mail server that can't be reached stopping the batch, the messages not tried waiting with it; a send that takes too long counting as a failure; at most `Outbox:MaxConcurrentSends` sends at once; a shutdown mid-batch keeping what was sent; **two dispatchers at once never sending the same message**; a retried email starting the schedule over and failing only after another day; the cleanup job deleting only what was sent more than 7 days ago. Through the API: the dispatcher sends both emails after the request has answered; without it, the request answers and both wait. FR-N2: the failed list (newest failure first, which email, to whom, the lead, why; the office's alert to the office; paginated); a retry due now with another day, recording who; 404 and 409 (Pending or Sent); two Admins retrying one email at once → one 204, one 409; Retry all retrying only the Failed ones and saying how many; the cleanup job scheduled daily | The schedule in unit tests; the batch with a clock the test sets and a stand-in mail server, each dispatcher with its own `DbContext`, released at the same moment; the API with its dispatcher running |
| **Lead emails** | What the welcome email and the office's alert say, in the site's words; that every value from the form stays text in their HTML (a name, address or note with markup adds no link, image or script); the welcome email saying a note reached the office without repeating it; both parts, the embedded wordmark and the office's `Reply-To`; the outbox's Message-ID | Renders the emails from a lead in memory; builds the MIME message without a mail server |
| **Settings tests** | Fail if a required setting can be missing or invalid without stopping the API at startup, or if the error doesn't name it ([operations](operations.md#configuration)) | Builds the services from in-memory settings and runs the startup validation |
| **Measurement harness** | Fail if the delivery log misses an email the mail server accepted, records one it didn't, or merges a re-send into the first row; if the fault lets an email through; or if Development stops wrapping the provider's sender with the logger outermost; if the outbox's emails reach the log without their message's id; if `Notifications:Mode` picks the wrong notifier, or the synchronous mode answers before both emails are sent or writes outbox messages ([measurements](measurements.md#4-measurement-harness)) | The senders alone and as Development wires them, on the test's database; an outbox batch through them; one lead request through the API in the synchronous mode |
| **Frontend unit** | The marketing site's logic: the quote form's checks, step by step; the request it sends (coordinates only from a picked address, never made up); the API's validation messages linked to their fields, and its other answers; the address suggestions kept (complete US addresses only), when a lookup is made, and how an earlier answer narrows as the visitor types on; each page's head. The staff app's logic: how each API answer is classified (kind, message, refused fields, a stale record), what a request sends and how its answer is checked, which failures are retried, a 401 ending the session, which state a load shows, the theme, the sign-in form's checks and messages, where signing in leads (the page to return to, only if it is safe and the account's role sees it), the navigation by role, the staff accounts' requests, the account forms' checks and where the API's refusals show (under a field or as one notice), the generated password (its length and kinds of characters, and no favoured characters), how dates are written | Vitest, in Node, with no rendering; the API and the address provider are not called. Before a screen is done, it is checked in a browser against the real API ([ADR 6](adr/0006-staff-app-stack.md)) |
| **Performance and fault** | The four measured problems, P1–P4 | Scripts in `perf/`, run by hand, results committed; not part of CI ([measurements](measurements.md)) |

---

## Conventions

- **Real PostgreSQL, never EF Core's in-memory provider.** The in-memory provider ignores unique indexes,
  transactions and concurrency tokens: exactly the behaviour this project depends on.
- **Each business rule has at least one test that names it**, e.g. `BR_L1_duplicate_open_lead_returns_existing`.
- **Each bug fix comes with a regression test** that fails without the fix.
- **Concurrency tests use real parallelism:** separate connections, released together, never sequential calls
  pretending to be simultaneous.

---

## Running the tests

`dotnet test` in `backend/` runs the unit and integration tests. The repository's `global.json` runs it on the
Microsoft Testing Platform, which xUnit v3 uses. The integration tests need Docker,
for the PostgreSQL container.

`npm test` in `frontend/marketing/` and in `frontend/staff/` runs each app's tests.

CI runs the same commands on every pull request and on `main` after each merge
([`.github/workflows/ci.yml`](../.github/workflows/ci.yml)): the API's restore, build (warnings are errors) and tests,
and each frontend's lint, tests and build ([operations](operations.md#build-and-ci)).
