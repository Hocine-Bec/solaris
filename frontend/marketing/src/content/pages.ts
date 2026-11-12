// Each page's title and description: shown in the browser, in search results and in link previews. Read by the app on
// every navigation and by the build, which writes each page's own HTML head (vite.config.ts).

export type PageMeta = {
  path: string;
  title: string;
  description: string;
  /** Kept out of search results: the confirmation and the not-found page. */
  noindex?: boolean;
};

export const SITE_NAME = 'Solaris Energy';

export const PAGES: PageMeta[] = [
  {
    path: '/',
    title: 'Solaris Energy — Solar for homes and small businesses across the US',
    description: 'Solar systems for homes and small businesses across the United States, installed in six steps you see before they start. Get a free quote.',
  },
  {
    path: '/how-it-works',
    title: 'How it works — Solaris Energy',
    description: 'From the site survey to switch-on: who comes, what happens and what you do at each of the six steps.',
  },
  {
    path: '/questions',
    title: 'Questions — Solaris Energy',
    description: 'Who can get solar, what it costs, how long it takes, and what happens after switch-on.',
  },
  {
    path: '/about',
    title: 'About — Solaris Energy',
    description: 'A solar installer for the whole United States, based in Austin, Texas: how we work, and how to reach us.',
  },
  {
    path: '/quote',
    title: 'Get a free quote — Solaris Energy',
    description: 'Tell us about your property in three short steps, and we contact you at the time you choose.',
  },
  {
    path: '/quote/sent',
    title: 'Request sent — Solaris Energy',
    description: 'What happens after a quote request.',
    noindex: true,
  },
  {
    path: '/privacy',
    title: 'Privacy — Solaris Energy',
    description: 'What the quote form collects, why, and who sees it.',
  },
];

export const NOT_FOUND: PageMeta = {
  path: '*',
  title: 'Page not found — Solaris Energy',
  description: "This page doesn't exist.",
  noindex: true,
};

/** The page at a path; a trailing slash doesn't matter. Any other path is the not-found page. */
export function pageFor(path: string): PageMeta {
  const clean = path.split(/[?#]/)[0].replace(/(.)\/+$/, '$1');
  return PAGES.find(page => page.path === clean) ?? NOT_FOUND;
}

const escape = (text: string) => text.replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);

/** A page's head tags as HTML, for the files the build writes (one per page, so link previews read the right ones). */
export function headTags(page: PageMeta): string {
  const title = escape(page.title), description = escape(page.description);
  return [
    `<title>${title}</title>`,
    `<meta name="description" content="${description}">`,
    `<meta property="og:type" content="website">`,
    `<meta property="og:site_name" content="${SITE_NAME}">`,
    `<meta property="og:title" content="${title}">`,
    `<meta property="og:description" content="${description}">`,
    `<meta name="twitter:card" content="summary">`,
    ...(page.noindex ? ['<meta name="robots" content="noindex">'] : []),
  ].join('\n    ');
}
