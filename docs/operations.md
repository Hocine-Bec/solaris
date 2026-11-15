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
| `Jwt__LifetimeHours` | `appsettings.json` (`8`) | How long a staff session lasts, 1 to 24 hours |
| `Admin__Name`, `Admin__Email`, `Admin__Password` | `.env` | The first Admin account, created at startup when no active Admin exists; the API stops if there is neither. All three or none; the password follows the password rule ([architecture](architecture.md#9-security)). Once an Admin exists they can be removed |
| `Cors__AllowedOrigins__0`, `__1`, … | `appsettings.Development.json` (`http://localhost:5173`) | The sites whose pages may send the quote form, as origins (`https://www.example.com`: scheme, host, optional port, no path). Empty outside Development: no other site may call the API. Any other endpoint answers no other site; the staff app needs none ([architecture](architecture.md#9-security)) |
| `Notifications__Mode` | `appsettings.json` (`Outbox`) | `Outbox`: the request saves a new lead's two emails as outbox messages, with the lead, and the outbox dispatcher sends them in the background ([architecture](architecture.md#6-notifications)). `Synchronous`: the request sends them itself before it answers, and writes no outbox messages; Development only, for measurements, and the API refuses it elsewhere |
| `Outbox__MaxConcurrentSends` | — | How many of a batch's emails the dispatcher sends at once, 1 to 20; 5 by default (Gmail refused emails from one account at about 10 at once). The P1 and P2 measurement runs set 20, as many as the Hangfire reference's workers |
| `Email__Provider` | `appsettings.json` (`Gmail`), `appsettings.Development.json` (`Recording`) | `Gmail` sends real email; `Recording` sends nothing, and the API refuses it in Production |
| `Email__FromName` | `appsettings.json` | The sender name shown on emails: `Solaris` |
| `Email__FromAddress` | `.env` | The sender's address; sales notifications are sent to it too |
| `Email__SiteUrl` | `appsettings.Development.json` (`http://localhost:5173`); the environment outside Development | The marketing site's address, for the links in emails (the welcome email links to its How it works page); required, absolute |
| `Email__SimulatedLatencyMs` | `appsettings.json` | `Recording` only: how long each email takes, 2000 ms by default |
| `Email__Smtp__Host`, `Email__Smtp__Port` | `appsettings.json` | `smtp.gmail.com`, 587 |
| `Email__Gmail__ClientId`, `Email__Gmail__ClientSecret`, `Email__Gmail__RefreshToken` | `.env` | Required only when `Email__Provider=Gmail` |
| `FaultInjection__Enabled` | — | Development only (the measurement harness): `true` starts the API with the email fault on; `false` by default |
| `RateLimiting__Enabled` | — | `true` by default: 5 quote forms and 10 password checks per minute per client address ([architecture](architecture.md#9-security)). `false` only in Development, for measurement runs (the scripts in `perf/` set it); the API refuses it elsewhere |
| `ForwardedHeaders__KnownNetworks__0`, `__1`, … | — | The proxies whose `X-Forwarded-For` and `X-Forwarded-Proto` the API believes, as networks (`10.0.0.0/8`, or `10.1.2.3/32` for one address). Empty by default: no forwarded header is believed, and the rate limits count the connection's address. Set where the API runs behind a proxy, once its addresses are known; an entry that isn't a network stops the API at startup |
| `Database__MigrateOnStartup` *(planned)* | — | Apply pending migrations at startup |
| `POSTGRES_PASSWORD` | `.env` | Not read by the API: docker compose gives it to the local database ([running locally](#running-locally)) |

---

## Running locally

Prerequisites: the .NET 10 SDK and Docker. Copy `.env.example` to `.env` at the repository root and fill it in. On
a database with no Admin yet, the `Admin__…` settings are required: the API creates the first Admin from them, and
stops if they are missing.

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

- **The API in a container** *(planned)*: comes with the Dockerfile ([Build and CI](#build-and-ci)).
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
  exception decides whether it is an error, so an expected duplicate logs nothing and a bug is logged once.
  ASP.NET Core and EF Core log at Warning and above only (`Serilog:MinimumLevel:Override` in `appsettings.json`): the
  request line above replaces ASP.NET Core's own, and SQL isn't logged, the outbox dispatcher's check every 2 s
  included. To see the SQL, set `Serilog__MinimumLevel__Override__Microsoft.EntityFrameworkCore.Database.Command` to
  `Information`; shells can't assign a name with dots, so through `env`, for example
  `env 'Serilog__MinimumLevel__Override__Microsoft.EntityFrameworkCore.Database.Command=Information' dotnet run`.
  Performance numbers come from the load tool, not from logs ([measurements](measurements.md)).

---

## Build and CI

- **Docker image** *(planned)*: a multi-stage `Dockerfile`, building with `dotnet/sdk:10.0` and running on
  `dotnet/aspnet:10.0`; docker compose gains an `api` service built from it, next to the database.
- **CI:** GitHub Actions ([`.github/workflows/ci.yml`](../.github/workflows/ci.yml)), on every pull request and on
  `main` after each merge, on GitHub's hosted Ubuntu runners. Three jobs run side by side:
  - **API:** .NET 10 → restore → build (warnings are errors) → unit and integration tests. Testcontainers starts
    PostgreSQL 18 in the runner's Docker.
  - **Marketing site** and **Staff app:** Node.js 26 → `npm ci` → lint → tests → build.

  The actions are pinned to a commit, with their release in a comment. The workflow can only read the repository.
  A newer push to the same branch cancels the run it replaces.

---

## Deployment *(planned)*

Railway: the API service plus a Railway PostgreSQL database, configured through environment variables. The API
reference is public; the Hangfire dashboard is off outside Development.
