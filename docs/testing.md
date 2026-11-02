# Testing

What is tested, at which level, and how. Business rules (`BR-…`) are defined in
[functional requirements](functional-requirements.md).

**Status:** today there are 10 unit tests (8 for the `Lead` and `Address` entities, and the architecture test's 2
cases, one each for `Domain` and `Application`) and 64 repository tests that run on EF Core's in-memory provider.
Everything else below is *(planned)*, including the move to real PostgreSQL.

---

## Projects

| Project | Holds |
|---|---|
| `Tests/UnitTests` | Tests of pure logic in `Domain` and `Application`, with no database |
| `Tests/IntegrationTests` | Tests against a real PostgreSQL: repositories, services, concurrency, the API |
| `Tests/TestUtilities` | Shared helpers: test-data factories (Bogus), the PostgreSQL fixture and a database reset helper *(planned)* |

---

## Levels

| Level | What | How |
|---|---|---|
| **Unit** | The lead and installation state machines (every from/to pair, allowed or rejected), stage conditions, health classification, expected production, reading validation, the outbox backoff schedule | xUnit + FluentAssertions, no database |
| **Integration** | Services and repositories against real constraints; the outbox write path; rollup recalculation | Testcontainers `postgres:17`, with the database reset between tests |
| **Concurrency** | 50 identical leads at once → 1 lead; two conversions of one lead → 1 success + 409; conflicting stage changes → 1 success + 409s; a stale `ETag` → 409; replayed reading batches → 0 duplicates; two dispatchers never send the same message | N parallel tasks, each with its own `DbContext` or `HttpClient`, released at the same moment, then the invariants are checked |
| **API and authorization** | Anonymous → 401; a technician on an admin route → 403; a device key accepted only for its own installation | `WebApplicationFactory` + Testcontainers |
| **Guard test** | Fails if any endpoint answers anonymously without being on the explicit list of public endpoints | Enumerates every endpoint through `EndpointDataSource` |
| **Architecture test** | Fails if `Domain` or `Application` reference EF Core, Npgsql, Hangfire or MailKit ([ADR 1](adr/0001-layered-solution.md)) | Reads each project's assembly references |
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

## Running the tests *(planned)*

`dotnet test` from the repository root runs the unit and integration tests. The integration tests need Docker,
for the PostgreSQL container. CI runs the same command on every pull request ([operations](operations.md)).
