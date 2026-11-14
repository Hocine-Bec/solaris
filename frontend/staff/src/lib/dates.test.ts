import { describe, expect, it } from 'vitest';
import { formatDay, formatMoment } from './dates';

// In Austin's time zone, so the tests don't depend on the machine's
const TZ = 'America/Chicago';
const NOW = new Date('2026-11-14T15:30:00Z');   // 09:30 in Austin, 14 Nov

describe('formatMoment', () => {
  it('says Today for the same day, in the time zone shown', () => {
    expect(formatMoment('2026-11-14T14:51:00Z', NOW, TZ)).toBe('Today 08:51');
  });

  it('gives the day and time earlier this year, on a 24-hour clock', () => {
    expect(formatMoment('2026-11-13T23:40:00Z', NOW, TZ)).toBe('13 Nov 17:40');
    // 03:10 UTC on the 14th is still the 13th in Austin
    expect(formatMoment('2026-11-14T03:10:00Z', NOW, TZ)).toBe('13 Nov 21:10');
  });

  it('adds the year before this one', () => {
    expect(formatMoment('2025-12-31T18:05:00Z', NOW, TZ)).toBe('31 Dec 2025, 12:05');
  });
});

describe('formatDay', () => {
  it('gives the day, the month and the year', () => {
    expect(formatDay('2026-11-12T16:00:00Z', TZ)).toBe('12 Nov 2026');
  });
});
