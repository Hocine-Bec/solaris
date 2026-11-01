# 1. Layered solution

**Status:** Accepted, recorded retroactively · **Last changed:** 2026-10-31

**Context:** The API mixes business rules (state machines, duplicate rules, health), persistence (PostgreSQL)
and delivery (HTTP, email, jobs). They change for different reasons, and the rules must be testable without
a database.

## Options
- **A.** Four projects, dependencies pointing inward: `Domain` (entities, rules) ← `Application` (use cases,
  interfaces) ← `Infrastructure` (EF Core, email, jobs) ← `WebAPI` (HTTP, wiring everything together).
- **B.** One web project, with a folder per technical layer.
- **C.** Vertical slices: one folder per feature, holding its endpoint, logic and data access.

## Decision
**A.** Business rules (the installation state machine, the health thresholds) are plain code, unit-tested
with no database. Infrastructure is swappable behind interfaces (for example, the email sender has Gmail,
recording and fault-injecting versions). The compiler enforces the direction through project references,
and an architecture test fails if `Domain` or `Application` reference EF Core, Npgsql, Hangfire or MailKit.

## Trade-offs
More projects and interfaces; a feature touches three or four projects.

## Alternative that could also work
**C.** Less ceremony per feature, and a good fit for CRUD-heavy apps. Here the same domain rules are shared
across features (transitions, cancellation and activation all use the state machine), which fits a shared
`Domain` project better.
