# 3. Result objects for expected outcomes, exceptions for the unexpected

**Status:** Accepted, recorded retroactively; error kinds and the global handler were added on 2026-10-31 ·
**Last changed:** 2026-10-31

**Context:** Services have expected failures ("not found", "already converted", "transition not allowed").
The API must turn them into the right HTTP responses without putting business logic in controllers.

## Options
- **A.** `Result` / `Result<T>` for expected outcomes, where a failure carries an **error kind** defined in
  `Application` (NotFound, Conflict, Validation, Forbidden). `WebAPI` maps each kind to an HTTP status and a
  ProblemDetails body. Unexpected errors are exceptions, handled once by a global handler.
- **B.** HTTP status codes inside the result, and a catch-all in every service method.
- **C.** Exceptions for everything (`NotFoundException`, `ConflictException`, …), mapped by one global handler.
- **D.** A result library (FluentResults, ErrorOr).

## Decision
**A.** Failure paths are visible in method signatures, and an expected outcome is not an exception.
`Domain` and `Application` know nothing about HTTP; the controller picks the success status (201 for a
create, 204 for an update, 200 otherwise). Every error response is a ProblemDetails (RFC 9457) with a stable
`code` (for example `lead.not_found`). Services don't catch everything, so a bug reaches the global handler
with its stack trace, and the client gets a clean 500 whose `traceId` matches the log entry.

## Trade-offs
Each controller chooses its own success status, so that choice is not centralised.

## Alternative that could also work
**C.** Common, with the leanest signatures. But expected outcomes like a duplicate lead become control flow
through exceptions, and a method's failure paths can't be seen without reading its body. B leaks HTTP into
the inner layers and hides bugs behind generic 500s; D needs a dependency for what a few dozen lines of our
own code already do.
