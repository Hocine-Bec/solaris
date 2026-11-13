# Solaris staff app

The app [Solaris](../../README.md)'s staff work in: the office (Admin) runs leads, customers, installations,
production, failed notifications and accounts; technicians see the installations they're assigned to.

**Status:** the app's frame is built: the navigation, the themes, the states every screen shares, and the client
that talks to the API. Its screens come with their features, starting with signing in; until then each section
shows what it will hold.

## Pages

| Route | Page |
|---|---|
| `/` | Opens Leads |
| `/leads`, `/customers`, `/installations`, `/production`, `/notifications`, `/users` | The office's six sections, in the side bar |
| Any other | Page not found, inside the frame |

## The frame

- **A side bar** with the six sections; below 768 px, a top bar whose menu button opens the same navigation in a
  drawer (a native `<dialog>`: Escape or a click outside closes it).
- **Themes:** Light, Dark or System, chosen in a menu (a native popover) at the bottom of the side bar, and kept
  in the browser for the next visit. System follows the device, as it changes. A script in `index.html` sets the
  theme before the first paint, so a dark device never flashes light first.
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

`QueryView` picks the state for a load (TanStack Query), so a screen draws only its data. Signed out and Not
allowed come with signing in and the roles.

## Talking to the API

- **On the app's own address:** the app calls `/api`. In development, Vite's server passes these requests to the
  API; in production, they will be served from one site. Staff sessions will travel in an HttpOnly
  cookie, which the browser sends by itself and the app's code never reads.
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
([running locally](../../docs/operations.md#running-locally)) on `http://localhost:5228`.

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

React 19 · TypeScript · Vite 7 · Tailwind CSS 4 · React Router 7 · TanStack Query 5 · Zod 4 · Lucide icons ·
Vitest ([ADR 6](../../docs/adr/0006-staff-app-stack.md))
