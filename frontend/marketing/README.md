# Solaris marketing site

The public website of [Solaris](../../README.md): a landing page, and a quote form that sends a visitor's request
to the Solaris API as a new lead.

## Pages

| Route | Page |
|---|---|
| `/` | The landing page |
| `/quote` | The quote form, in three steps (contact details, property, qualification), then a confirmation |

The property step suggests addresses as the visitor types, using Geoapify's address autocomplete.

## Screenshots

- Landing page: [hero](screenshots/1.hero-section.png), [about](screenshots/2.about-section.png),
  [solutions](screenshots/3.solutions-section.png), [how it works](screenshots/4.how-it-works-section.png),
  [testimonials](screenshots/5.testimonials-section.png), [why choose us](screenshots/6.why-choose-us-section.png),
  [blog](screenshots/7.blog-section.png), [call to action and footer](screenshots/9.cta-footer-section.png)
- [The quote form](screenshots/10.quote-form-page.png)

## Running locally

Prerequisites: Node.js 20.19+ or 22.12+ (required by Vite 7), and the Solaris API running.

```sh
cp .env.example .env
npm install
npm run dev
```

## Configuration

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | The API's base URL, including `/api` (locally `http://localhost:5228/api`) |
| `VITE_GEOAPIFY_API_KEY` | A Geoapify API key for the address suggestions; without it, the address field shows a warning |

Vite builds both values into the JavaScript sent to the browser, so neither can be a secret.

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Starts the development server, with hot reload |
| `npm run build` | Type-checks, then builds the site into `dist/` |
| `npm run preview` | Serves the built `dist/` locally |
| `npm run lint` | Runs ESLint |

## Stack

React 19 · TypeScript · Vite 7 · Tailwind CSS 4 · React Router 7
