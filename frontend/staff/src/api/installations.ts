import { z } from 'zod';
import { request, requestTagged, type Tagged } from './client';
import { CUSTOMER_STATUSES, INSTALLATION_STATUSES, type InstallationStatus } from './customers';
import { addressSchema } from './leads';

export const ASSIGNMENT_ROLES = ['Surveyor', 'Installer', 'Inspector'] as const;
export type AssignmentRole = (typeof ASSIGNMENT_ROLES)[number];

const person = z.object({ id: z.number().int(), name: z.string() });

/** An installation in the list (GET /api/installations) */
export const installationSummarySchema = z.object({
  id: z.number().int(),
  address: addressSchema,
  customer: person,
  status: z.enum(INSTALLATION_STATUSES),
  createdAt: z.iso.datetime(),
  statusChangedAt: z.iso.datetime(),
});
export type InstallationSummary = z.infer<typeof installationSummarySchema>;

export const installationsPageSchema = z.object({
  items: z.array(installationSummarySchema),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalCount: z.number().int(),
});
export type InstallationsPage = z.infer<typeof installationsPageSchema>;

/**
 * One installation, as its page shows it (GET /api/installations/{id}, and every change's answer): the next move and,
 * while it waits, the API's reason; what can be done now; the design and the time zone, fixed once it is Active; its
 * assignments, each saying whether it can be completed or removed now; and its history, newest first.
 */
export const installationSchema = z.object({
  id: z.number().int(),
  address: addressSchema,
  customer: z.object({ id: z.number().int(), name: z.string(), status: z.enum(CUSTOMER_STATUSES) }),
  leadId: z.number().int(),
  status: z.enum(INSTALLATION_STATUSES),
  createdAt: z.iso.datetime(),
  activatedAt: z.iso.datetime().nullable(),
  timeZone: z.object({ id: z.string(), name: z.string(), fixed: z.boolean() }),
  timeZones: z.array(z.object({ id: z.string(), name: z.string() })),
  design: z.object({ systemSizeKw: z.number().nullable(), panelCount: z.number().int().nullable(), inverterType: z.string().nullable(), fixed: z.boolean() }),
  next: z.object({ status: z.enum(INSTALLATION_STATUSES), allowed: z.boolean(), reason: z.string().nullable() }).nullable(),
  canCancel: z.boolean(),
  canDeactivate: z.boolean(),
  /** How its device key starts and when it was made, while it is Active and has one: never the key (BR-T2) */
  deviceKey: z.object({ prefix: z.string(), issuedAt: z.iso.datetime() }).nullable(),
  assignments: z.array(z.object({
    id: z.number().int(),
    technician: person,
    role: z.enum(ASSIGNMENT_ROLES),
    assignedAt: z.iso.datetime(),
    assignedBy: person.nullable(),
    completedAt: z.iso.datetime().nullable(),
    completedBy: person.nullable(),
    canComplete: z.boolean(),
    canRemove: z.boolean(),
  })),
  history: z.array(z.object({
    at: z.iso.datetime(),
    kind: z.enum(['Status', 'Assigned', 'Completed']),
    from: z.enum(INSTALLATION_STATUSES).nullable(),
    to: z.enum(INSTALLATION_STATUSES).nullable(),
    by: person.nullable(),
    note: z.string().nullable(),
    technician: person.nullable(),
    role: z.enum(ASSIGNMENT_ROLES).nullable(),
  })),
});
export type Installation = z.infer<typeof installationSchema>;
export type Assignment = Installation['assignments'][number];
export type HistoryEntry = Installation['history'][number];

export const INSTALLATIONS_PAGE_SIZE = 20;
export const INSTALLATIONS_KEY = ['installations'] as const;
export const installationsPageKey = (statuses: readonly InstallationStatus[], page: number) =>
  [...INSTALLATIONS_KEY, 'page', statuses.join(','), page] as const;
export const installationKey = (id: number) => [...INSTALLATIONS_KEY, id] as const;

/** One page of the installations in these statuses (all when none is given), the last change first. */
export function fetchInstallations(statuses: readonly InstallationStatus[], page: number, signal?: AbortSignal) {
  const query = new URLSearchParams([...statuses.map(s => ['status', s]), ['page', String(page)], ['pageSize', String(INSTALLATIONS_PAGE_SIZE)]]);
  return request(`/installations?${query}`, { schema: installationsPageSchema, signal });
}

/** The installation with its version, which every change sends back (`If-Match`, BR-I4). */
export const fetchInstallation = (id: number, signal?: AbortSignal): Promise<Tagged<Installation>> =>
  requestTagged(`/installations/${id}`, { schema: installationSchema, signal });

/**
 * A change's answer: the installation with its version and, only from switching on or replacing the key, the device
 * key itself (BR-T2). The key is apart from the installation so that it is shown once and never kept: the page puts
 * `data` and `etag` in the query cache, never `issuedKey`.
 */
export type Changed = Tagged<Installation> & { issuedKey: string | null };
const changedSchema = installationSchema.extend({ issuedKey: z.string().optional() });

type Change = { id: number; etag: string | null };
const change = async (path: string, method: 'POST' | 'PUT' | 'DELETE', { id, etag }: Change, body?: unknown): Promise<Changed> => {
  const { data: { issuedKey, ...data }, etag: version } =
    await requestTagged(`/installations/${id}${path}`, { method, body, ifMatch: etag, schema: changedSchema });
  return { data, etag: version, issuedKey: issuedKey ?? null };
};

/** The next stage, when its condition is met (BR-I1) */
export const moveInstallation = (c: Change, status: InstallationStatus) => change('/status', 'POST', c, { status });
export const cancelInstallation = (c: Change, reason: string) => change('/cancel', 'POST', c, { reason });
export const deactivateInstallation = (c: Change, note: string | null) => change('/deactivate', 'POST', c, { note });
export type Design = { systemSizeKw: number; panelCount: number; inverterType: string | null };
export const recordDesign = (c: Change, design: Design) => change('/design', 'PUT', c, design);
export const setTimeZone = (c: Change, timeZone: string) => change('/time-zone', 'PUT', c, { timeZone });
export const assignTechnician = (c: Change, technicianId: number, role: AssignmentRole) =>
  change('/assignments', 'POST', c, { technicianId, role });
export const removeAssignment = (c: Change, assignmentId: number) => change(`/assignments/${assignmentId}`, 'DELETE', c);
/** An Admin marks a technician's part complete for them (FR-I6) */
export const completeAssignment = (c: Change, assignmentId: number) => change(`/assignments/${assignmentId}/complete`, 'POST', c);
/** A new device key, shown once; the one it had stops working (BR-T2) */
export const replaceDeviceKey = (c: Change) => change('/device-key', 'POST', c);
