import { describe, expect, it } from 'vitest';
import { opensFor, sectionsFor } from './sections';

describe('sectionsFor', () => {
  it("shows the office its six sections, in the design's order", () => {
    expect(sectionsFor('Admin').map(s => s.path)).toEqual(['/leads', '/customers', '/installations', '/production', '/notifications', '/users']);
  });

  it('shows a technician only their own work', () => {
    expect(sectionsFor('Technician').map(s => s.path)).toEqual(['/my-work']);
  });
});

describe('opensFor', () => {
  it("opens the office's pages, with their records and filters, for the office", () => {
    expect(opensFor('Admin', '/leads')).toBe(true);
    expect(opensFor('Admin', '/installations/12/production?month=2026-10')).toBe(true);
    expect(opensFor('Admin', '/production?x=1')).toBe(true);
  });

  it("doesn't open another role's pages", () => {
    expect(opensFor('Technician', '/production?x=1')).toBe(false);
    expect(opensFor('Technician', '/my-work/12')).toBe(true);
    expect(opensFor('Admin', '/my-work')).toBe(false);
  });

  it('matches whole sections, not prefixes of their names', () => {
    expect(opensFor('Admin', '/leadsy')).toBe(false);
    expect(opensFor('Admin', '/unknown')).toBe(false);
  });
});
