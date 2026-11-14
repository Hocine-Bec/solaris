# 9. Closed by default: every endpoint needs a signed-in account unless it opens itself

**Status:** Accepted · **Last changed:** 2026-11-14

**Context:** Until staff sign-in was built, no endpoint required a session: the API had a token configuration but no
`[Authorize]` anywhere, so the leads, with people's names, emails, phone numbers and addresses, answered anyone. The
staff app brings two roles, the office (Admin) and technicians. Every endpoint added from now on has to be protected,
and forgetting to protect one must not leave it open.

## Options
- **A.** A fallback authorization policy that requires a signed-in account for every endpoint without a rule of its
  own. An endpoint open to anyone says so with `[AllowAnonymous]`; named policies (`Admin`, `Technician`, `Staff`)
  narrow an endpoint to roles, reading the token's `role` claim.
- **B.** `[Authorize]` on each controller or action; endpoints are open unless someone remembers to close them.
- **C.** A convention or global filter that adds `[Authorize]` to every MVC controller.

## Decision
**A.** A new endpoint is closed until someone opens it on purpose, and opening one is a single attribute a review can
see. Open to anyone today: the quote form (`POST /api/leads`), signing in and signing out; in Development only, the
API reference and the measurement harness's switch, which no other environment maps. The leads are the office's:
`Admin` on the controller, the quote form opting out. A technician calling an office endpoint gets `403`.

## Trade-offs
The fallback also covers addresses that don't exist: signed out, an unknown URL answers `401`, not `404`, so a caller
can't tell which addresses exist; signed in, it is `404`. Middleware that isn't an endpoint, such as the Hangfire
dashboard, isn't covered and keeps its own rule. A test that lists every endpoint open to anyone, and fails on a new
one, is what keeps the list honest *(planned)*.

## Alternative that could also work
**C** closes MVC controllers but not minimal API endpoints, such as the measurement switch, so it would need a second
mechanism next to it. **B** is common, and it is how the API ended up with every endpoint open.
