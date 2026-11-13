# Operations

How Solaris is configured, run, observed, built and deployed.

**Status:** most of this is *(planned)*. Built today: typed configuration, the local database in docker compose,
migrations applied automatically in Development, the email provider switch (the Gmail settings are required only
when Gmail sends), and the notifications mode.

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
| `Notifications__Mode` | `appsettings.json` (`Hangfire`) | `Hangfire`: the request enqueues the two lead emails as Hangfire jobs. `Synchronous`: the request sends them itself before it answers; Development only, for measurements, and the API refuses it elsewhere. `Outbox` *(planned)* replaces `Hangfire` as the default |
| `Email__Provider` | `appsettings.json` (`Gmail`), `appsettings.Development.json` (`Recording`) | `Gmail` sends real email; `Recording` sends nothing, and the API refuses it in Production |
| `Email__FromName` | `appsettings.json` | The sender name shown on emails: `Solaris` |
| `Email__FromAddress` | `.env` | The sender's address; sales notifications are sent to it too |
| `Email__SimulatedLatencyMs` | `appsettings.json` | `Recording` only: how long each email takes, 2000 ms by default |
| `Email__Smtp__Host`, `Email__Smtp__Port` | `appsettings.json` | `smtp.gmail.com`, 587 |
| `Email__Gmail__ClientId`, `Email__Gmail__ClientSecret`, `Email__Gmail__RefreshToken` | `.env` | Required only when `Email__Provider=Gmail` |
| `FaultInjection__Enabled` | — | Development only (the measurement harness): `true` starts the API with the email fault on; `false` by default |
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
- **The frontends** (Node.js 22.13+, 24 or 26+): `npm install`, then `npm run dev`, in each app's folder.
  - `frontend/marketing/`, on `http://localhost:5173`, calls the API at its `VITE_API_BASE_URL`
    ([configuration](../frontend/marketing/README.md#configuration)).
  - `frontend/staff/`, on `http://localhost:5174`, calls `/api` on its own address; its development server passes
    those requests to the API on `http://localhost:5228` (`API_PROXY_TARGET` changes it,
    [configuration](../frontend/staff/README.md#configuration)).

In Development, the API sends no real email: `Email__Provider` is `Recording`, so the Gmail settings can stay out
of `.env`. To send real emails locally, set `Email__Provider=Gmail` with the three Gmail secrets. Development also
runs the measurement harness ([measurements](measurements.md#4-measurement-harness)).

In Development, the API reference (Scalar) is served under `/scalar`, from the OpenAPI document at
`/openapi/v1.json`. Demo data comes from the dataset generator
([measurements](measurements.md)), not from seed code.

---

## Database migrations

EF Core migrations live in `Infrastructure`. They are applied at startup when `Database__MigrateOnStartup=true`,
which suits a single-instance deployment *(planned)*. Production gets no demo seeding.

The EF Core tools build the model through a design-time factory (`DesignTimeDbContextFactory`), so they don't need
the API's settings. They need the API as the startup project, since it references `Microsoft.EntityFrameworkCore.Design`.
Commands that reach a database take the connection string explicitly:

```sh
dotnet ef database update --project backend/src/Infrastructure --startup-project backend/src/WebAPI \
  --connection "Host=…;Database=…;Username=…;Password=…"
```

---

## Health and logging

- **Health checks** *(planned)*: `/health/live` (the process is up) and `/health/ready` (the database is
  reachable). Both are public.
- **Logging:** Serilog structured logs, including a line for every request (method, path, status, elapsed time), written
  to the console and to daily-rolling files. Every line written during a request ends with its trace id, which is
  the middle part of an error response's `traceId` (`00-<trace id>-…`), so a 500 leads to its exception. EF Core's
  own failure events (a failed command, save, query or connection) are logged at Debug: the code that catches the
  exception decides whether it is an error, so an expected duplicate logs nothing and a bug is logged once. Performance numbers come from the load tool, not from logs
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
