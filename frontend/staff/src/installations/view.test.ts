import { describe, expect, it } from 'vitest';
import { ApiError } from '../api/errors';
import type { HistoryEntry } from '../api/installations';
import { filterFrom, FILTERS, historyBy, historyLine, installationsHref, isRefusal, kw } from './view';

const rosa = { id: 1, name: 'Rosa Castillo' };
const jamal = { id: 7, name: 'Jamal Ortiz' };
const entry = (e: Partial<HistoryEntry>): HistoryEntry =>
  ({ at: '2026-11-21T10:00:00Z', kind: 'Status', from: null, to: null, by: null, note: null, technician: null, role: null, ...e });

describe('the installation list filters', () => {
  it('opens on All, also for an unknown filter, with the others beside it', () => {
    expect(filterFrom(null).key).toBe('all');
    expect(filterFrom('everything').key).toBe('all');
    expect(FILTERS.map(f => f.label)).toEqual(['All', 'In progress', 'Survey', 'Design', 'Permits', 'Installation', 'Inspection', 'Active', 'Closed']);
  });

  it('asks the API for the statuses each filter gathers, and keeps it in the address', () => {
    expect(filterFrom('progress').statuses).toEqual(['Survey', 'Design', 'Permits', 'Installation', 'Inspection']);
    expect(filterFrom('closed').statuses).toEqual(['Cancelled', 'Deactivated']);
    expect(installationsHref(filterFrom('permits'), 2)).toBe('/installations?status=permits&page=2');
  });
});

describe('the history in words', () => {
  it('says what happened and who did it', () => {
    expect([historyLine(entry({ to: 'Survey', by: rosa })), historyBy(entry({ to: 'Survey', by: rosa }))]).toEqual(['Created at Survey', 'By Rosa Castillo']);
    expect(historyLine(entry({ from: 'Design', to: 'Permits' }))).toBe('Design → Permits');
    expect(historyLine(entry({ kind: 'Assigned', technician: jamal, role: 'Surveyor', by: rosa }))).toBe('Jamal Ortiz assigned as Surveyor');
    expect(historyLine(entry({ kind: 'Completed', technician: jamal, role: 'Inspector' }))).toBe('Jamal Ortiz completed the inspection');
  });

  it('says when an Admin marked a part complete for the technician (FR-I6)', () => {
    expect(historyBy(entry({ kind: 'Completed', technician: jamal, role: 'Surveyor', by: jamal }))).toBe('Marked by them');
    expect(historyBy(entry({ kind: 'Completed', technician: jamal, role: 'Surveyor', by: rosa }))).toBe('Marked by Rosa Castillo for them');
  });
});

describe('a refused change', () => {
  it("is the API's refusal of an installation's or an assignment's change, not a change made meanwhile", () => {
    const refused = (code: string) => new ApiError('conflict', 'Refused.', { status: 409, code });
    expect(isRefusal(refused('installation.transition_not_allowed'))).toBe(true);
    expect(isRefusal(refused('assignment.not_at_its_stage'))).toBe(true);
    expect(isRefusal(refused('installation.concurrent_change'))).toBe(false);
    expect(isRefusal(refused('lead.email_taken'))).toBe(false);
  });

  it('writes a size in kW', () => {
    expect(kw(8.4)).toBe('8.4 kW');
    expect(kw(10)).toBe('10 kW');
  });
});
