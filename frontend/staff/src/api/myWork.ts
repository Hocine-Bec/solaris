import { z } from 'zod';
import { request, requestTagged, type Tagged } from './client';
import { INSTALLATION_STATUSES } from './customers';
import { ASSIGNMENT_ROLES } from './installations';
import { addressSchema } from './leads';

/** One of a technician's assignments, in My work (GET /api/my-work), or on their page under Users */
export const myWorkItemSchema = z.object({
  installationId: z.number().int(),
  address: addressSchema,
  customerName: z.string(),
  status: z.enum(INSTALLATION_STATUSES),
  role: z.enum(ASSIGNMENT_ROLES),
  assignmentId: z.number().int(),
  completedAt: z.iso.datetime().nullable(),
});
export type MyWorkItem = z.infer<typeof myWorkItemSchema>;

/** A technician's assignment, on their page under Users (GET /api/users/{id}/assignments, FR-U4) */
export const userAssignmentSchema = myWorkItemSchema.omit({ assignmentId: true });
export type UserAssignment = z.infer<typeof userAssignmentSchema>;

/** One installation as its technician sees it (GET /api/my-work/{id}): the customer to call, their part, the others' */
export const myInstallationSchema = z.object({
  installationId: z.number().int(),
  address: addressSchema,
  customer: z.object({ name: z.string(), phone: z.string() }),
  status: z.enum(INSTALLATION_STATUSES),
  myAssignments: z.array(z.object({
    id: z.number().int(), role: z.enum(ASSIGNMENT_ROLES), completedAt: z.iso.datetime().nullable(), canComplete: z.boolean(), why: z.string().nullable(),
  })),
  team: z.array(z.object({ name: z.string(), role: z.enum(ASSIGNMENT_ROLES), completedAt: z.iso.datetime().nullable() })),
});
export type MyInstallation = z.infer<typeof myInstallationSchema>;

export const MY_WORK_KEY = ['my-work'] as const;
export const myInstallationKey = (id: number) => [...MY_WORK_KEY, id] as const;
export const userAssignmentsKey = (userId: number) => ['users', userId, 'assignments'] as const;

export const fetchMyWork = (signal?: AbortSignal) => request('/my-work', { schema: z.array(myWorkItemSchema), signal });

export const fetchMyInstallation = (id: number, signal?: AbortSignal): Promise<Tagged<MyInstallation>> =>
  requestTagged(`/my-work/${id}`, { schema: myInstallationSchema, signal });

/** The technician marks their own part complete (FR-I6) */
export const completeMine = (id: number, assignmentId: number, etag: string | null) =>
  requestTagged(`/my-work/${id}/assignments/${assignmentId}/complete`, { method: 'POST', ifMatch: etag, schema: myInstallationSchema });

export const fetchUserAssignments = (userId: number, signal?: AbortSignal) =>
  request(`/users/${userId}/assignments`, { schema: z.array(userAssignmentSchema), signal });
