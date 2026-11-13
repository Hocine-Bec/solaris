# 6. The staff app's stack and its test level

**Status:** Accepted · **Last changed:** 2026-11-12

**Context:** The staff app is the repository's second frontend, beside the marketing site. Every screen loads
data from the API and has the same loading, error and refresh states; the business rules live in the backend and
are tested there.

## Options
- **A.** The marketing site's toolchain (React, TypeScript, Vite, Tailwind CSS, React Router, ESLint, Vitest),
  plus TanStack Query for server data and Zod to check the API's answers. Vitest unit tests for the client's
  logic, in Node, with no rendering; a browser check against the real API before each feature is done.
- **B.** A newer toolchain for the staff app alone: TanStack Router (typed search params), Vite 8, TypeScript 6,
  oxlint.
- **C.** Plain `fetch` and `useState` on each screen, with no data library.
- **D.** A, plus component tests (Testing Library) or end-to-end tests (Playwright).

## Decision
**A.** One toolchain, upgraded one way, for both apps. TanStack Query handles loading, errors, retries and
refreshing the same way on every screen, so the shared states are built once (`QueryView`). Zod checks each
answer where it arrives, so a change in the API's shape fails as one clear error instead of a wrong value deep in a
screen. The rules are tested in the backend; the client's own logic (how an error is classified, which state a
load shows, what a request sends) is where frontend bugs would hurt, and those tests run in Node in milliseconds.

## Trade-offs
Nothing checks the rendered screens automatically: a broken layout or a lost focus is caught by the browser check,
not by CI. The two apps have their own lockfiles, kept at the same versions by hand.

## Alternative that could also work
**B.** Typed search params suit lists whose filters live in the address, as the staff app's do; but the repository
would have two toolchains to upgrade. **D** is the step to take if the browser checks stop catching what breaks:
Playwright first, against the real API. C repeats the same loading and error code on every screen.
