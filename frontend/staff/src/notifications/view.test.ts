import { describe, expect, it } from 'vitest';
import type { FailedNotification } from '../api/notifications';
import { emailName, oneLine, stillFailed, toLine } from './view';

const welcome: FailedNotification = {
  id: 'a', type: 'LeadWelcome', to: { name: 'Maya Brooks', address: 'maya.brooks@example.com' }, lead: { id: 128, name: 'Maya Brooks' }, installation: null,
  createdAt: '2026-11-14T08:51:00Z', lastAttemptAt: '2026-11-15T08:51:00Z', attempts: 14, lastError: null, retriedAt: null, retriedBy: null,
};
const alert: FailedNotification = { ...welcome, id: 'b', type: 'SalesNewLeadAlert', to: { name: 'Solaris', address: 'sales@solaris.example' } };

describe('the rows', () => {
  it('names each email and to whom it goes, the alert by the lead it is about', () => {
    expect([emailName(welcome), toLine(welcome)]).toEqual(['Welcome email', 'To Maya Brooks, maya.brooks@example.com']);
    expect([emailName(alert), toLine(alert)]).toEqual(['New lead alert', 'To the office, about Maya Brooks']);
  });

  it("names a switch-on email, to the installation's customer", () => {
    const switchOn: FailedNotification = {
      ...welcome, id: 'c', type: 'InstallationActivated', to: { name: 'Grace Kim', address: 'grace.kim@example.com' },
      lead: null, installation: { id: 35, street: '61 Pecan St' },
    };
    expect([emailName(switchOn), toLine(switchOn)]).toEqual(['Switch-on email', 'To Grace Kim, grace.kim@example.com']);
  });

  it('puts the error on one line', () => {
    expect(oneLine('Failed to refresh Gmail access token: {\n  "error": "invalid_grant"\n}'))
      .toBe('Failed to refresh Gmail access token: { "error": "invalid_grant" }');
    expect(oneLine(null)).toBe('No error was recorded.');
  });

  it('counts the failed emails still waiting: the total, less those retried from the screen', () => {
    expect(stillFailed(23, [welcome, alert], new Set(['a']))).toBe(22);
    expect(stillFailed(23, [welcome, alert], new Set(['a', 'not shown']))).toBe(22);
  });
});
