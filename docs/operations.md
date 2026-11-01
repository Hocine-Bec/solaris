# Operations

How Solaris is configured, run, observed, built and deployed.

**Status:** most of this is *(planned)*. Today the API reads differently named environment variables
(`DEFAULTCONNECTION`, `SECRET_KEY`, …) from a `.env` file, requires the Gmail settings at startup, and applies
migrations and demo seed data automatically in Development only.

---

## Configuration *(planned)*

Standard ASP.NET Core configuration: `appsettings.json` holds non-secret defaults, and environment variables
override them using `Section__Key` names. Each section binds to a typed options class validated at startup, so
a missing or invalid setting stops the API with a clear message. Code never reads environment variables
directly.

Secrets are never committed: locally they live in `dotnet user-secrets` (when running the API directly) or in
docker-compose's own `.env` file; in production they are environment variables.

| Key | Values / notes |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__LifetimeHours` | Lifetime 8 hours |
| `Admin__Email`, `Admin__Password` | The first Admin account, created at startup if none exists |
| `Cors__AllowedOrigins` | The marketing site's URL |
| `Notifications__Mode` | `Outbox` (default) \| `Synchronous` (Development only, for measurements) |
| `Email__Provider` | `Gmail` \| `Recording` |
| `Email__FromName`, `Email__FromAddress` | The sender shown on emails |
| `Email__SimulatedLatencyMs` | `Recording` only |
| `Email__Smtp__Host`, `Email__Smtp__Port` | `Gmail` only |
| `Email__Gmail__ClientId`, `Email__Gmail__ClientSecret`, `Email__Gmail__RefreshToken` | Required only when `Email__Provider=Gmail` |
| `FaultInjection__Enabled` | Development only, for measurements |
| `RateLimiting__Enabled` | `true` by default; `false` only for measurement runs |
| `Database__MigrateOnStartup` | Apply pending migrations at startup |

---

## Running locally *(planned)*

Prerequisites: the .NET 10 SDK and Docker.

- **Everything in containers:** `docker compose up` starts PostgreSQL 17 and the API, configured from a `.env`
  file next to `docker-compose.yml` (copy `.env.example`).
- **The API from your IDE or `dotnet run`:** start only the database with `docker compose up postgres`, and keep
  the API's secrets in `dotnet user-secrets`.

The API reference (Scalar) is served under `/scalar`. Demo data comes from the dataset generator
([measurements](measurements.md)), not from seed code.

---

## Database migrations

EF Core migrations live in `Infrastructure`. They are applied at startup when `Database__MigrateOnStartup=true`,
which suits a single-instance deployment *(planned)*. Production gets no demo seeding.

---

## Health and logging

- **Health checks** *(planned)*: `/health/live` (the process is up) and `/health/ready` (the database is
  reachable). Both are public.
- **Logging:** Serilog structured logs, including a line for every request (method, path, status, elapsed time), written
  to the console and to daily-rolling files. Performance numbers come from the load tool, not from logs
  ([measurements](measurements.md)).

---

## Build and CI *(planned)*

- **Docker image:** a multi-stage `Dockerfile`, building with `dotnet/sdk:10.0` and running on `dotnet/aspnet:10.0`.
- **CI:** GitHub Actions on every pull request and on `main`: set up .NET 10 → restore → build → unit and
  integration tests. Testcontainers runs on the hosted Ubuntu runners.

---

## Deployment *(planned)*

Railway: the API service plus a Railway PostgreSQL database, configured through environment variables. The API
reference is public; the Hangfire dashboard is off outside Development.
