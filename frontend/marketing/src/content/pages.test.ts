import { describe, expect, it } from 'vitest';
import { headTags, NOT_FOUND, pageFor } from './pages';

describe('pageFor', () => {
  it('finds a page by its path, with or without a trailing slash or a query', () => {
    expect(pageFor('/about').title).toBe('About — Solaris Energy');
    expect(pageFor('/about/').title).toBe('About — Solaris Energy');
    expect(pageFor('/quote?from=home').title).toBe('Get a free quote — Solaris Energy');
    expect(pageFor('/').path).toBe('/');
  });

  it('answers the not-found page for any other path', () => {
    expect(pageFor('/no-such-page')).toBe(NOT_FOUND);
  });
});

describe('headTags', () => {
  it("writes the page's title, description and link-preview tags", () => {
    const html = headTags(pageFor('/questions'));
    expect(html).toContain('<title>Questions — Solaris Energy</title>');
    expect(html).toContain('<meta property="og:title" content="Questions — Solaris Energy">');
    expect(html).toContain('<meta name="description" content="Who can get solar, what it costs, how long it takes, and what happens after switch-on.">');
    expect(html).not.toContain('noindex');
  });

  it('keeps the confirmation and the not-found page out of search results', () => {
    expect(headTags(pageFor('/quote/sent'))).toContain('<meta name="robots" content="noindex">');
    expect(headTags(NOT_FOUND)).toContain('<meta name="robots" content="noindex">');
  });

  it('escapes quotes in the values', () => {
    expect(headTags({ path: '/x', title: 'A "quoted" title', description: 'Fish & chips' }))
      .toContain('content="A &quot;quoted&quot; title"');
  });
});
