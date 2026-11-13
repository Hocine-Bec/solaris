# Solaris staff app

The app [Solaris](../../README.md)'s staff work in: the office (Admin) runs leads, customers, installations,
production, failed notifications and accounts; technicians see the installations they're assigned to.

**Status:** signing in and the app's frame are built: the session, the navigation by role, the account menu, the
themes, the states every screen shares, and the client that talks to the API. The other screens come with their
features; until then each section shows what it will hold.

## Pages

| Route | Page |
|---|---|
| `/login` | Signing in (`?returnTo=` remembers the page to come back to) |
| `/` | Opens the account's landing page: Leads for the office, My work for technicians |
| `/leads`, `/customers`, `/installations`, `/production`, `/notifications`, `/users` | The office's six sections, in the side bar |
| `/my-work` | A technician's work |
| Any other | Page not found, inside the frame |

Every page but `/login` needs a session: without one, the sign-in page opens and remembers the address.

## Signing in

- **The page:** one card, in the chosen theme. The form checks that both fields are filled and the email looks like
  one; the API answers the rest. Every refusal shows one message, "The email or password is incorrect.", as the API
  answers the same way for an unknown email, a wrong password or a deactivated account. After a refusal the email
  stays, the password is cleared and hidden, and focus moves to it.
- **Beside it, on large screens** (1024 px wide and more, in landscape): the house from the home page's How it works,
  drawing itself through the six steps, about 20 seconds a round, with the step names below it. At switch-on the sun
  rises beside the roof, its light reaches the panels, they light up in a wave from its side, then the windows glow;
  while the house holds, the sun gives off slow rings. The drawing is decoration (`aria-hidden`); with the system's
  reduced-motion setting it shows the switched-on house, still. Below that size, the card alone.
- **The password field:** paste works, so password managers do (the fields say `autocomplete="username"` and
  `"current-password"`); a button shows the password; a warning appears when Caps Lock is on.
- **Where it leads:** back to the page the session ended on, if it is an address inside the app that the account's
  role sees (a page left by one person never opens for the next); otherwise the account's landing page. Opening the
  sign-in page while signed in goes straight there.
- **The session:** the API keeps it in an HttpOnly cookie for eight hours; the app's code never sees it. When the app
  opens, and when its window comes back into focus, it asks the API who is signed in (`GET /api/auth/me`).
- **When it ends:** a 401 on any request means the session ended or the account was deactivated: the sign-in page
  opens, says "Your session ended", and comes back to the page after signing in.
- **Signing out:** from the account menu, at once. The API clears the cookie and the sign-in page says "You're signed
  out." If the API doesn't answer, the person stays signed in, as the cookie can only be cleared by the API.

## The frame

- **A side bar** with the account's sections (the office's six, or My work for a technician); below 768 px, a top bar
  whose menu button opens the same navigation in a drawer (a native `<dialog>`: Escape or a click outside closes it).
  The navigation only hides links: the API checks the role on every request.
- **The account menu,** from the account button at the bottom of the side bar (initials, name and role), or from the
  initials in the phone's top bar: the email, the theme, and Sign out, in a native popover.
- **Themes:** Light, Dark or System, chosen in the account menu, and kept in the browser for the next visit. System
  follows the device, as it changes. A script in `index.html` sets the theme before the first paint, so a dark device
  never flashes light first, the sign-in page included.
- **Focus:** after moving to a new screen, focus goes to its title, so a screen reader announces where you are. A
  "Skip to the content" link comes first.
- **Each page's title** shows in the browser's tab ("Leads · Solaris").

## The states every screen shares

Built once in `src/states/`; each screen gives only its own words:

| State | When |
|---|---|
| Loading | Until the first answer: rows in the shape of what is coming, no spinner over the page |
| Empty | The list has nothing; the screen says why and what fills it |
| Failed | The network or the server failed; Try again keeps the screen's filters. A later refresh that fails leaves the data shown |
| Not found | A wrong link, or a record that no longer exists (404) |
| Changed by someone else | A 409 because the record changed since it was read: nothing is overwritten, and Reload shows the latest |

`QueryView` picks the state for a load (TanStack Query), so a screen draws only its data. Signed out is the
sign-in page; Not allowed comes with the roles' checks in the API.

## Talking to the API

- **On the app's own address:** the app calls `/api`. In development, Vite's server passes these requests to the
  API; in production, they will be served from one site. The session travels in an HttpOnly cookie, which the
  browser sends by itself and the app's code never reads.
- **Every failure is an `ApiError`** (`src/api/errors.ts`) with a kind a screen acts on: unreachable, invalid,
  signed out, forbidden, not found, conflict, too many requests, server, or unexpected (an answer the app doesn't
  understand). It carries the API's message when the API wrote one for people, the fields it refused (by their JSON
  names), its code and its trace id.
- **Answers are checked:** a request with a Zod schema fails as unexpected if the JSON doesn't match it, instead of
  passing a wrong shape to the screen.
- **Retries:** a load is tried once more when the network or the server failed; a 4xx answer is final. A change is
  never retried, so it can't be applied twice.

## Design

From the approved staff app design. The tokens, the core components, the app's components (text scale, segmented
choice, menu, tables, states) and the fonts come from [`../brand/`](../brand/README.md); the frame's layout is in
`src/styles/shell.css`. Icons are Lucide, at a 1.5 line.

## Running locally

Prerequisites: Node.js 22.13+, 24 or 26+, and the Solaris API running
([running locally](../../docs/operations.md#running-locally)) on `http://localhost:5228`, with the first Admin's
settings (`Admin__Name`, `Admin__Email`, `Admin__Password`) to sign in with.

Open the app at `http://localhost:5174`. The session cookie is `Secure`, which browsers accept over plain HTTP only
from the local machine, so a phone on the network can't keep a session until the app is served over HTTPS.

```sh
npm install
npm run dev        # http://localhost:5174
```

## Configuration

| Variable | Purpose |
|---|---|
| `API_PROXY_TARGET` | Where the API runs, for the development and preview servers (`http://localhost:5228` if unset). Set it in `.env.local` (see `.env.example`). Read by the server only, never built into the app |

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Starts the development server, with hot reload |
| `npm run build` | Type-checks, then builds the app into `dist/` |
| `npm run preview` | Serves the built `dist/` locally, with the same `/api` pass-through |
| `npm test` | Runs the unit tests ([testing](../../docs/testing.md)) |
| `npm run lint` | Runs ESLint |

## Stack

React 19 · TypeScript · Vite 7 · Tailwind CSS 4 · React Router 7 · TanStack Query 5 · React Hook Form 7 · Zod 4 ·
Lucide icons · Vitest ([ADR 6](../../docs/adr/0006-staff-app-stack.md))
