# Solaris marketing site

The public website of [Solaris](../../README.md): who Solaris is, the six steps of an installation, the questions
people ask, and a quote form that sends a visitor's request to the Solaris API as a new lead. Solaris Energy is
fictional, and the site says so: in its footer, on About, on the form and after it.

## Pages

| Route | Page |
|---|---|
| `/` | Home: one long scroll, from the hero to the six steps, what you get, who it's for, the questions asked most often and getting started |
| `/how-it-works` | Each of the six steps: who comes, what happens, what the customer does |
| `/questions` | All the questions, by topic |
| `/about` | Who Solaris is, its four promises, and how to reach it |
| `/quote` | The quote form, in three steps: contact, property, last details |
| `/quote/sent` | What happens next (kept out of search results) |
| `/privacy` | What the form collects, why, and who sees it |
| Any other | Page not found (kept out of search results) |

Each page has its own title, description and link-preview tags. The build writes one HTML file per page
(`about/index.html`, …) plus `404.html`, so a shared link previews the right page without running JavaScript; the
app keeps them up to date as the visitor moves around.

## Screenshots

- Home: [the hero](screenshots/1-home-hero.png), [how it works](screenshots/2-home-how-it-works.png),
  [what you get](screenshots/3-home-what-you-get.png),
  [getting started and the footer](screenshots/4-home-call-to-action-and-footer.png)
- [How it works](screenshots/5-how-it-works.png)
- [The quote form, with address suggestions](screenshots/6-quote-form.png)
- [The home page on a phone](screenshots/7-phone-home.png)

## The quote form

- **Three steps,** each checked before the next. The API checks everything again; if it refuses an answer, its own
  message is shown at the top of the form, linked to the field.
- **The request:** `POST /api/leads`. The state is chosen from the 50 states and DC and sent as its two-letter code;
  the country is `US`. A renter of a house or an apartment is told the owner has to ask, as the API requires (business
  premises may be leased).
- **Address suggestions,** from Geoapify's address autocomplete, called from the browser once the text starts with a
  house number and the visitor pauses between words (a street or a city alone returns no complete address, so it
  gets a hint instead of a lookup). Only complete US addresses are offered. Geoapify takes a second or more to
  answer, so the field never waits: a lookup isn't cancelled by the next keystroke, the list narrows as the visitor
  types, each answer is kept for the visit, and a spinner shows while one is on its way. Picking an address fills
  the street, city, state and ZIP code; typing an address without picking works too. The coordinates are sent only with a picked address
  that hasn't been changed since, never guessed. Without a key, or once the service fails, the street is a plain
  field. Geoapify's free plan allows 3,000 requests a day and asks for its credit and OpenStreetMap's next to the
  suggestions, which the list shows. The privacy page names Geoapify.
- **When sending fails** (no answer, too many requests, a server error, an address that already has a request), a
  message at the top says what happened; nothing entered is lost.

## Design

The tokens, the core components' styles, the fonts, the logo and the line drawings come from
[`../brand/`](../brand/README.md). Sections
alternate the Night and Limestone themes. The motion is written by hand, in CSS and a few scroll-driven effects, with
no animation library: the intro on the home page (the wordmark draws itself, then flies into the header; any click,
key, scroll or touch skips it), the hero's sun and panel, the pinned how-it-works section (at switch-on the sun rises and lights the house), the
drawings that draw themselves. With the system's reduced-motion setting, nothing moves.

## Running locally

Prerequisites: Node.js 22.13+, 24 or 26+ (the versions Vite 7, ESLint 10 and Vitest 5 all support), and the Solaris
API running ([running locally](../../docs/operations.md#running-locally)).

```sh
cp .env.example .env.local    # then fill it in
npm install
npm run dev
```

## Configuration

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | The API's base URL, including `/api` (locally `http://localhost:5228/api`); `/api` on the site's own address if unset |
| `VITE_GEOAPIFY_API_KEY` | A Geoapify API key for the address suggestions; without one, the street is a plain field |

Vite builds both values into the JavaScript sent to the browser, so neither can be a secret: in Geoapify's
dashboard, limit the key to the site's addresses.

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Starts the development server, with hot reload |
| `npm run build` | Type-checks, then builds the site into `dist/` |
| `npm run preview` | Serves the built `dist/` locally (it maps `/about/`, with the slash, to `about/index.html`) |
| `npm test` | Runs the unit tests ([testing](../../docs/testing.md)) |
| `npm run lint` | Runs ESLint |

## Hosting

`npm run build` writes a static site to `dist/`. The host should serve a page's own file for its path (`/about` →
`about/index.html`) and `404.html` for any other path. A host that sends every path to `index.html` works too, but
link previews then show the home page's title.

## Stack

React 19 · TypeScript · Vite 7 · Tailwind CSS 4 · React Router 7 · Lucide icons · Vitest
