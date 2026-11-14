/*
 * Dates as the staff app writes them, in the browser's time zone: "Today 08:51", "13 Nov 17:40", "12 Nov 2026".
 * Day before month and a 24-hour clock, as in the approved designs. `timeZone` is for tests.
 */

const parts = (date: Date, timeZone: string | undefined, options: Intl.DateTimeFormatOptions) =>
  new Intl.DateTimeFormat('en-GB', { ...options, timeZone }).format(date);

const time = (date: Date, timeZone?: string) => parts(date, timeZone, { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' });
const dayKey = (date: Date, timeZone?: string) => parts(date, timeZone, { year: 'numeric', month: '2-digit', day: '2-digit' });
const year = (date: Date, timeZone?: string) => parts(date, timeZone, { year: 'numeric' });

/** A day: "12 Nov 2026" */
export const formatDay = (iso: string, timeZone?: string) =>
  parts(new Date(iso), timeZone, { day: 'numeric', month: 'short', year: 'numeric' });

/**
 * A moment, as short as it can be read: "Today 08:51" today, "13 Nov 17:40" this year, "13 Nov 2025, 17:40" before.
 */
export function formatMoment(iso: string, now: Date = new Date(), timeZone?: string): string {
  const date = new Date(iso);
  if (dayKey(date, timeZone) === dayKey(now, timeZone)) return `Today ${time(date, timeZone)}`;
  const day = parts(date, timeZone, { day: 'numeric', month: 'short' });
  return year(date, timeZone) === year(now, timeZone) ? `${day} ${time(date, timeZone)}` : `${day} ${year(date, timeZone)}, ${time(date, timeZone)}`;
}
