import type { FailedNotification } from '../api/notifications';

/** "Welcome email", "New lead alert": the email, in the office's words */
export const emailName = (n: FailedNotification) => (n.type === 'LeadWelcome' ? 'Welcome email' : 'New lead alert');

/** To whom: the person and their address, or for the alert, the office and the lead it is about */
export const toLine = (n: FailedNotification) =>
  n.type === 'LeadWelcome' ? `To ${n.to.name}, ${n.to.address}` : `To the office, about ${n.lead.name}`;

/** The error on one line, for the row: its line breaks and runs of spaces as single spaces */
export const oneLine = (error: string | null) => (error ?? 'No error was recorded.').replace(/\s+/g, ' ').trim();

/** The failed emails still waiting on the screen: the total, less those retried from it */
export const stillFailed = (totalCount: number, shown: readonly FailedNotification[], retried: ReadonlySet<string>) =>
  totalCount - shown.filter(n => retried.has(n.id)).length;
