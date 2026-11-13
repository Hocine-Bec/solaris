# Testing

What is tested, at which level, and how. Business rules (`BR-…`) are defined in
[functional requirements](functional-requirements.md).

**Status:** today there are 39 unit tests (8 for the `Lead` and `Address` entities, 1 for the list of states, 13 for
the lead request's state code and coordinates, 12 for the password rule, 3 for the sign-in request, and the
architecture test's 2 cases, one each for `Domain` and `Application`) and 178 integration tests: 149 on PostgreSQL 18
(75 for the repositories, 2 for the unit of work, 5 for `LeadService`, 1 for the schema's names, 14 for the email
senders and the measurement harness, and 52 through the API for error responses, pagination, timestamps, the email
fault toggle, the synchronous notifications, and signing in: the session cookie, `/me`, signing out, requests from
another site and the first Admin), and 29 for the settings' startup validation, which need no database. The marketing
site has 68 unit tests of its own, and the staff app 95 (below). Everything else below is *(planned)*.

---

## Projects

| Project | Holds |
|---|---|
| `backend/tests/UnitTests` | Tests of pure logic in `Domain` and `Application`, with no database |
| `backend/tests/IntegrationTests` | Tests against a real PostgreSQL: repositories, services, concurrency, the API |
| `backend/tests/TestUtilities` | Shared helpers: test-data factories (Bogus) and the PostgreSQL fixture, which gives each test its own database |
| `frontend/marketing/src/**/*.test.ts` | The marketing site's unit tests, beside the code they test: the quote form's checks and the request it builds (25), how each answer of the API is shown (11), which address suggestions are offered and how the list narrows as the visitor types (27), each page's title and description (5) |
| `frontend/staff/src/**/*.test.ts` | The staff app's unit tests, beside the code they test: how each answer of the API becomes an error a screen acts on, from answers recorded from the API and the answers of a gateway that couldn't reach it (25), the requests the client sends and how it checks the answers (9), which failures are retried and how a 401 ends the session (13), signing in and asking who is signed in (5), the sign-in form's checks and messages (11), where signing in leads, the page to return to and the initials (19), the navigation by role (5), the theme choice (4), which state a load shows (4) |

---

## Levels

| Level | What | How |
|---|---|---|
| **Unit** | The lead and installation state machines (every from/to pair, allowed or rejected), stage conditions, health classification, expected production, reading validation, the outbox backoff schedule | xUnit + FluentAssertions, no database |
| **Integration** | Services and repositories against real constraints; the outbox write path; rollup recalculation | Testcontainers `postgres:18`: one container per test run; each test gets its own database, copied from a template that has the migrations applied, so tests run in parallel without sharing data |
| **Concurrency** | 50 identical leads at once → 1 lead; two conversions of one lead → 1 success + 409; conflicting stage changes → 1 success + 409s; a stale `ETag` → 409; replayed reading batches → 0 duplicates; two dispatchers never send the same message | N parallel tasks, each with its own `DbContext` or `HttpClient`, released at the same moment, then the invariants are checked |
| **API** | Every error response is a ProblemDetails with its code and a `traceId`, and a bug's 500 shows no exception details ([architecture](architecture.md#5-request-handling)); lists are paginated, with their limits; timestamps without a zone are rejected and the others stored as UTC; signing in sets the session cookie with its attributes, every refusal gets the same answer, `/me` follows the account (a deactivation stops its session), signing out clears the cookie, a hash with fewer iterations is replaced at sign-in, a change from another site with the cookie is refused while the public form isn't, and the first Admin is created from the settings, once | `WebApplicationFactory` + Testcontainers: the real API in memory, outside Development, on the test's own database; only the email queue is a fake |
| **Authorization** *(planned)* | Anonymous → 401; a technician on an admin route → 403; a device key accepted only for its own installation | `WebApplicationFactory` + Testcontainers |
| **Guard test** | Fails if any endpoint answers anonymously without being on the explicit list of public endpoints | Enumerates every endpoint through `EndpointDataSource` |
| **Architecture test** | Fails if `Domain` or `Application` reference EF Core, Npgsql, Hangfire or MailKit ([ADR 1](adr/0001-layered-solution.md)) | Reads each project's assembly references |
| **Schema naming test** | Fails if any table, column, constraint or index in the migrated database isn't snake_case (EF Core's own `__EFMigrationsHistory` table excepted) | Reads PostgreSQL's catalog in a test database |
| **Settings tests** | Fail if a required setting can be missing or invalid without stopping the API at startup, or if the error doesn't name it ([operations](operations.md#configuration)) | Builds the services from in-memory settings and runs the startup validation |
| **Measurement harness** | Fail if the delivery log misses an email the mail server accepted, records one it didn't, or merges a re-send into the first row; if the fault lets an email through; or if Development stops wrapping the provider's sender with the logger outermost; if `Notifications:Mode` picks the wrong notifier, or the synchronous mode answers before both emails are sent ([measurements](measurements.md#4-measurement-harness)) | The senders alone and as Development wires them, on the test's database; the current Hangfire jobs called directly; one lead request through the API in the synchronous mode |
| **Frontend unit** | The marketing site's logic: the quote form's checks, step by step; the request it sends (coordinates only from a picked address, never made up); the API's validation messages linked to their fields, and its other answers; the address suggestions kept (complete US addresses only), when a lookup is made, and how an earlier answer narrows as the visitor types on; each page's head. The staff app's logic: how each API answer is classified (kind, message, refused fields, a stale record), what a request sends and how its answer is checked, which failures are retried, a 401 ending the session, which state a load shows, the theme, the sign-in form's checks and messages, where signing in leads (the page to return to, only if it is safe and the account's role sees it), the navigation by role | Vitest, in Node, with no rendering; the API and the address provider are not called. Before a screen is done, it is checked in a browser against the real API ([ADR 6](adr/0006-staff-app-stack.md)) |
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
for the PostgreSQL container. CI will run the same command on every pull request *(planned)* ([operations](operations.md)).

`npm test` in `frontend/marketing/` and in `frontend/staff/` runs each app's tests.
