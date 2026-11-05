# Testing

What is tested, at which level, and how. Business rules (`BR-…`) are defined in
[functional requirements](functional-requirements.md).

**Status:** today there are 10 unit tests (8 for the `Lead` and `Address` entities, and the architecture test's 2
cases, one each for `Domain` and `Application`) and 86 integration tests: 71 on PostgreSQL 18 (64 for the
repositories, 2 for the unit of work, 4 for `LeadService` and 1 for the schema's names), and 15 for the settings'
startup validation, which need no database. Everything else below is *(planned)*.

---

## Projects

| Project | Holds |
|---|---|
| `backend/tests/UnitTests` | Tests of pure logic in `Domain` and `Application`, with no database |
| `backend/tests/IntegrationTests` | Tests against a real PostgreSQL: repositories, services, concurrency, the API |
| `backend/tests/TestUtilities` | Shared helpers: test-data factories (Bogus) and the PostgreSQL fixture, which gives each test its own database |

---

## Levels

| Level | What | How |
|---|---|---|
| **Unit** | The lead and installation state machines (every from/to pair, allowed or rejected), stage conditions, health classification, expected production, reading validation, the outbox backoff schedule | xUnit + FluentAssertions, no database |
| **Integration** | Services and repositories against real constraints; the outbox write path; rollup recalculation | Testcontainers `postgres:18`: one container per test run; each test gets its own database, copied from a template that has the migrations applied, so tests run in parallel without sharing data |
| **Concurrency** | 50 identical leads at once → 1 lead; two conversions of one lead → 1 success + 409; conflicting stage changes → 1 success + 409s; a stale `ETag` → 409; replayed reading batches → 0 duplicates; two dispatchers never send the same message | N parallel tasks, each with its own `DbContext` or `HttpClient`, released at the same moment, then the invariants are checked |
| **API and authorization** | Anonymous → 401; a technician on an admin route → 403; a device key accepted only for its own installation | `WebApplicationFactory` + Testcontainers |
| **Guard test** | Fails if any endpoint answers anonymously without being on the explicit list of public endpoints | Enumerates every endpoint through `EndpointDataSource` |
| **Architecture test** | Fails if `Domain` or `Application` reference EF Core, Npgsql, Hangfire or MailKit ([ADR 1](adr/0001-layered-solution.md)) | Reads each project's assembly references |
| **Schema naming test** | Fails if any table, column, constraint or index in the migrated database isn't snake_case (EF Core's own `__EFMigrationsHistory` table excepted) | Reads PostgreSQL's catalog in a test database |
| **Settings tests** | Fail if a required setting can be missing or invalid without stopping the API at startup, or if the error doesn't name it ([operations](operations.md#configuration)) | Builds the services from in-memory settings and runs the startup validation |
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
