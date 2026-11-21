import { ApiError } from '../api/errors';
import type { InstallationStatus } from '../api/customers';
import type { AssignmentRole, HistoryEntry } from '../api/installations';

/** The stages in order, as the tracker draws them (BR-I1) */
export const STAGES = ['Survey', 'Design', 'Permits', 'Installation', 'Inspection', 'Active'] as const satisfies readonly InstallationStatus[];

/** The list's filters, in its address (`/installations?status=permits`): each one the statuses it asks for */
export const FILTERS = [
  { key: 'all', label: 'All', statuses: [] },
  { key: 'progress', label: 'In progress', statuses: ['Survey', 'Design', 'Permits', 'Installation', 'Inspection'] },
  ...(['Survey', 'Design', 'Permits', 'Installation', 'Inspection'] as const).map(s => ({ key: s.toLowerCase(), label: s, statuses: [s] })),
  { key: 'active', label: 'Active', statuses: ['Active'] },
  { key: 'closed', label: 'Closed', statuses: ['Cancelled', 'Deactivated'] },
] as const satisfies readonly { key: string; label: string; statuses: readonly InstallationStatus[] }[];
export type Filter = (typeof FILTERS)[number];

/** The filter an address asks for; the list opens on All (Hocine, 2026-11-21). */
export const filterFrom = (value: string | null): Filter => FILTERS.find(f => f.key === value) ?? FILTERS[0];

export const installationsHref = (filter: Filter, page = 1) =>
  `/installations?status=${filter.key}${page > 1 ? `&page=${page}` : ''}`;

/** What the empty list says */
export const emptyFor = (filter: Filter) =>
  filter.key === 'all'
    ? { title: 'No installations yet', text: 'An installation starts when a lead is converted.' }
    : filter.key === 'closed'
      ? { title: 'No closed installations', text: 'Cancelled and deactivated installations appear here.' }
      : { title: `No installations ${filter.key === 'progress' ? 'in progress' : `at ${filter.label}`}`, text: 'Installations appear here when they reach this stage.' };

/** What each role does, in the technician's words, on their page */
export const PART: Record<AssignmentRole, { noun: string; does: string; then: string }> = {
  Surveyor: {
    noun: 'survey',
    does: "Visit the property and check the roof's condition, its orientation, the shade on it and the electrical panel.",
    then: 'The office moves the installation to Design once the survey is complete.',
  },
  Installer: {
    noun: 'part',
    does: 'Install the panels, the inverter and the wiring, with the crew.',
    then: "The office moves the installation to Inspection once every installer's part is complete.",
  },
  Inspector: {
    noun: 'inspection',
    does: 'Check that the finished installation is safe and compliant.',
    then: 'The office switches the system on once the inspection is complete.',
  },
};

/** One line of the history, for its row: what happened */
export function historyLine(entry: HistoryEntry): string {
  if (entry.kind === 'Status')
    return entry.from ? `${entry.from} → ${entry.to}` : `Created at ${entry.to}`;
  const who = entry.technician?.name ?? 'A technician';
  return entry.kind === 'Assigned'
    ? `${who} assigned as ${entry.role}`
    : `${who} completed the ${entry.role ? PART[entry.role].noun : 'part'}${entry.role === 'Installer' ? ' of the installation' : ''}`;
}

/** Who did it, for its row: "By Rosa Castillo", or for a part marked by someone else, "Marked by Rosa Castillo for them" */
export function historyBy(entry: HistoryEntry): string | null {
  if (!entry.by) return null;
  if (entry.kind === 'Completed')
    return entry.by.id === entry.technician?.id ? 'Marked by them' : `Marked by ${entry.by.name} for them`;
  return `By ${entry.by.name}`;
}

/**
 * The API refused a change because of the installation's state: a move its condition doesn't allow yet, a part not
 * at its stage, something another Admin changed meanwhile. The page says why and loads the installation again.
 */
export const isRefusal = (error: unknown): error is ApiError =>
  error instanceof ApiError && error.kind === 'conflict' && !!error.code && !error.code.endsWith('.concurrent_change')
  && (error.code.startsWith('installation.') || error.code.startsWith('assignment.'));

/** "8.4 kW" */
export const kw = (value: number) => `${value.toLocaleString('en-US', { maximumFractionDigits: 2 })} kW`;
