# 2. Services and repositories, with one save per operation

**Status:** Accepted, recorded retroactively; the one-save-per-operation rule was added on 2026-10-31 ·
**Last changed:** 2026-10-31

**Context:** Use cases (create a lead, convert it, move an installation forward) live in `Application`
services, which reach the database through one repository interface per entity. When every repository call
saves on its own, a use case that writes several rows can't make them succeed or fail together, and two
use cases need exactly that: a lead with its notifications, and a conversion that creates a customer and an
installation.

## Options
- **A.** Services + repositories, where **repositories never save**: each service operation ends with one
  save through a unit of work, and one save is one database transaction.
- **B.** No repositories: services query an EF Core context interface directly.
- **C.** CQRS with MediatR: one handler class per command and per query.
- **D.** Repositories that save on every call, plus explicit transactions where several writes must go
  together.

## Decision
**A.** One rule: **one service operation is one transaction.** Repositories add, change and query, but never
save; use-case-specific queries live in each entity's repository (for example, "find the open lead for this
email"). The unit of work translates EF Core errors (unique violation, concurrency conflict) into outcomes
`Application` understands, so services never see EF types (ADR 1). A service doesn't call another service's
write method; it uses the repositories, so its writes stay inside its own transaction.

## Trade-offs
Interfaces on top of EF Core, which is already a unit of work and a repository itself, so more code than B.
Each new query needs a repository method.

## Alternative that could also work
**B.** Fewer layers and full LINQ in services; many teams choose it. Here it would make `Application` depend
on EF Core, which breaks ADR 1. C adds ceremony with no measurable benefit at this size; D spreads
transaction handling across services, where it is easy to forget.
