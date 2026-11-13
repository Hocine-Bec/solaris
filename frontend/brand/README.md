# Solaris brand

The design system's code, shared by Solaris's frontends: the tokens, the base styles and core components, the fonts,
the logo and the line drawings. The [marketing site](../marketing/README.md) and the [staff app](../staff/README.md) use it.

| File | Holds |
|---|---|
| `tokens.css` | The palette, the roles each theme gives it, type, space, radii and motion, for Tailwind CSS 4 (`@theme`) |
| `components.css` | The base styles and the core components: text styles, links, buttons, fields, notices |
| `app-components.css` | The components for the staff app, on top of `components.css`: the app's text scale, the segmented choice, the menu, avatars, tables, and the loading, empty and failed states |
| `fonts.css`, `fonts/` | The fonts, as WOFF2, with their licences |
| `logo/` | The wordmark, on dark and on light, and its outlines as JSON |
| `drawings/iso.ts` | The line drawings, in isometric projection, as data: the house with one layer per step, and the figures (a panel, the inverter, the paperwork, someone watching) |
| `drawings/drawable.ts` | Prepares a drawing's SVG to draw itself, each line from its own length |
| `drawings.css` | The drawings' shapes and ink in each theme, the house's steps, drawing itself, and its switch-on (the sun rising, the panels lighting up, the windows glowing) |

## Using it

In the app's main stylesheet, after Tailwind:

```css
@import "tailwindcss";
@import "<path>/brand/tokens.css";
@import "<path>/brand/components.css";
@import "<path>/brand/drawings.css";         /* an app that shows the line drawings */
@import "<path>/brand/app-components.css";   /* the staff app */
```

And in the app's entry script: `import '<path>/brand/fonts.css';`. The fonts are imported from JavaScript because
Tailwind inlines the stylesheets it imports without rebasing their `url()`s; imported by Vite, the font files are
found and bundled. Vite's development server must be allowed to read this folder (`server.fs.allow`).

The drawings are data and plain functions, with no packages: this folder has no `node_modules`, so it can't import
React. Each app turns a drawing into an SVG with its own small `DrawingSvg` component, and sizes and places it.

## Themes

Two themes, set on any element with `data-theme`: `night` (Night background, white text, the yellow as accent) and
`limestone` (Limestone background, near-black text, a deep yellow for text and lines on it). Components use the
roles (`--bg`, `--fg`, `--muted`, `--faint`, `--line`, `--accent`, `--accent-fill`, …), never the palette directly.
The yellow never means a status: info, positive, warning and negative have their own colours.

## Fonts

| Font | File | Kept |
|---|---|---|
| Archivo | `archivo-variable.woff2` | Variable: weight 400 to 700, width 100% to 125% |
| Fraunces Italic | `fraunces-italic.woff2` | One style: weight 400, optical size 144, SOFT 0, WONK 1 |
| IBM Plex Mono, as Solaris Mono | `solaris-mono-regular.woff2`, `solaris-mono-medium.woff2` | Regular and Medium |

Each file keeps Basic Latin, Latin-1 (for Western European names) and the punctuation, arrows and signs the copy uses,
with the standard OpenType features, and Archivo's tabular figures (the Mono's figures are all one width anyway). They
were made with fontTools 4.68.0 and brotli 1.2.1 from the families' files in Google Fonts' repository: instanced to
the weights and widths the designs use, trimmed to those characters, then compressed. Checked in Chrome against the
original files: Fraunces and the Mono draw the same pixels; Archivo is exact at weight 600 and within 1.22 units of
1,000 elsewhere, as instancing rounds outlines to whole units. The four files total 99,984 bytes.

**Why "Solaris Mono":** IBM Plex's licence (the SIL Open Font License 1.1) reserves the name "Plex", and a trimmed
font is a modified version, which may not use a reserved name (OFL FAQ 2.5 and 2.6). These files are IBM Plex Mono
2.3 under a new name, with IBM's copyright and the licence kept (`fonts/OFL-IBMPlexMono.txt`). Archivo and Fraunces
reserve no names.

## Logo

SOLARIS in Archivo, weight 600, width 125%, with a letter spacing of 0.005 em, and the sun (`#ffc21f`) as its full
stop. The outlines come from the font (fontTools); HarfBuzz (uharfbuzz 0.57.0) placed the letters, kerning included.
All three files share the view box `-2 26 598.2 75`, with the baseline at y = 100.

| File | Use |
|---|---|
| `logo/solaris-wordmark-on-dark.svg` | White letters (`#f2f2f0`), on Night |
| `logo/solaris-wordmark-on-light.svg` | Black letters (`#0b0b0c`), on Limestone |
| `logo/wordmark.json` | Each letter's position and contours, for drawing the wordmark line by line (the home page's intro) |
