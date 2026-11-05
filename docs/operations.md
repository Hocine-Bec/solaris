# Operations

How Solaris is configured, run, observed, built and deployed.

**Status:** most of this is *(planned)*. Built today: typed configuration, the local database in docker compose, and
migrations applied automatically in Development. The API still requires the Gmail settings at startup.

---

## Configuration

Standard ASP.NET Core configuration, from two places:

- **`backend/src/WebAPI/appsettings.json`** (committed): non-secret defaults only, such as the JWT issuer and the
  SMTP host.
- **Environment variables**, which override it using `Section__Key` names: the secrets, and anything specific to
  one machine. Locally they come from one gitignored `.env` file at the repository root (copy `.env.example`),
  which the IDE and docker compose load; in production, the hosting platform sets them. The API never reads a
  `.env` file itself.

Each section binds to a typed options class, validated as soon as the API is built: a missing or invalid setting
stops it before it touches the database, and the error names every setting at fault. Code never reads environment
variables directly, and secrets never go in `appsettings.json`.

| Key | Where | Values / notes |
|---|---|---|
| `ConnectionStrings__Default` | `.env` | PostgreSQL connection string |
| `Jwt__Secret` | `.env` | The token signing key, at least 32 characters |
| `Jwt__Issuer`, `Jwt__Audience` | `appsettings.json` | `solaris-api`, `solaris-staff` |
| `Jwt__LifetimeHours` *(planned)* | — | 8 hours |
| `Admin__Email`, `Admin__Password` *(planned)* | — | The first Admin account, created at startup if none exists |
| `Cors__AllowedOrigins` *(planned)* | — | The marketing site's URL |
| `Notifications__Mode` *(planned)* | — | `Outbox` (default) \| `Synchronous` (Development only, for measurements) |
| `Email__Provider` *(planned)* | — | `Gmail` \| `Recording` |
| `Email__FromName` | `appsettings.json` | The sender name shown on emails: `Solaris` |
| `Email__FromAddress` | `.env` | The sender's address; sales notifications are sent to it too |
| `Email__SimulatedLatencyMs` *(planned)* | — | `Recording` only |
| `Email__Smtp__Host`, `Email__Smtp__Port` | `appsettings.json` | `smtp.gmail.com`, 587 |
| `Email__Gmail__ClientId`, `Email__Gmail__ClientSecret`, `Email__Gmail__RefreshToken` | `.env` | Required today; required only when `Email__Provider=Gmail` *(planned)* |
| `FaultInjection__Enabled` *(planned)* | — | Development only, for measurements |
| `RateLimiting__Enabled` *(planned)* | — | `true` by default; `false` only for measurement runs |
| `Database__MigrateOnStartup` *(planned)* | — | Apply pending migrations at startup |
| `POSTGRES_PASSWORD` | `.env` | Not read by the API: docker compose gives it to the local database ([running locally](#running-locally)) |

---

## Running locally

Prerequisites: the .NET 10 SDK and Docker. Copy `.env.example` to `.env` at the repository root and fill it in.

- **The database:** `docker compose up -d` at the repository root starts PostgreSQL 18, with the password from
  `.env` (`POSTGRES_PASSWORD`, which the connection string repeats). It listens on `localhost:5433`, for this
  machine only, so it never collides with another PostgreSQL on 5432. It restarts with Docker, and its data stays in
  the `postgres-data` volume until `docker compose down -v`. Any PostgreSQL client connects with the same host, port,
  user (`postgres`) and password.
- **The API from VS Code:** a launch configuration with `"envFile"` pointing at the repository's `.env` loads it
  into the API's environment.
- **The API from a terminal:** load the file into the shell, then run the API from its folder:

  ```sh
  set -a; . ./.env; set +a          # at the repository root
  cd backend/src/WebAPI && dotnet run
  ```

- **The API in a container** *(planned)*: comes with the Dockerfile ([Build and CI](#build-and-ci-planned)).

In Development, the API reference (Scalar) is served under `/scalar`, from the OpenAPI document at
`/openapi/v1.json`. Demo data comes from the dataset generator
([measurements](measurements.md)), not from seed code.

---

## Database migrations

EF Core migrations live in `Infrastructure`. They are applied at startup when `Database__MigrateOnStartup=true`,
which suits a single-instance deployment *(planned)*. Production gets no demo seeding.

The EF Core tools build the model through a design-time factory (`DesignTimeDbContextFactory`), so they don't need
the API's settings. Commands that reach a database take the connection string explicitly:

```sh
dotnet ef database update --project backend/src/Infrastructure --connection "Host=…;Database=…;Username=…;Password=…"
```

---

## Health and logging

- **Health checks** *(planned)*: `/health/live` (the process is up) and `/health/ready` (the database is
  reachable). Both are public.
- **Logging:** Serilog structured logs, including a line for every request (method, path, status, elapsed time), written
  to the console and to daily-rolling files. Performance numbers come from the load tool, not from logs
  ([measurements](measurements.md)).

---

## Build and CI *(planned)*

- **Docker image:** a multi-stage `Dockerfile`, building with `dotnet/sdk:10.0` and running on `dotnet/aspnet:10.0`;
  docker compose gains an `api` service built from it, next to the database.
- **CI:** GitHub Actions on every pull request and on `main`: set up .NET 10 → restore → build → unit and
  integration tests. Testcontainers runs on the hosted Ubuntu runners.

---

## Deployment *(planned)*

Railway: the API service plus a Railway PostgreSQL database, configured through environment variables. The API
reference is public; the Hangfire dashboard is off outside Development.
