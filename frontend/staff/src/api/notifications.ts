import { z } from 'zod';
import { request } from './client';

export const NOTIFICATION_TYPES = ['LeadWelcome', 'SalesNewLeadAlert', 'InstallationActivated'] as const;
export type NotificationType = (typeof NOTIFICATION_TYPES)[number];

/** An email the outbox couldn't send in its 24 hours of tries (GET /api/notifications/failed) */
export const failedNotificationSchema = z.object({
  id: z.guid(),
  type: z.enum(NOTIFICATION_TYPES),
  /** The person, or for the office's alert, the office; for a switch-on email, the customer */
  to: z.object({ name: z.string(), address: z.string() }),
  /** What it is about: a lead's emails name the lead; a switch-on email, its installation */
  lead: z.object({ id: z.number().int(), name: z.string() }).nullable(),
  installation: z.object({ id: z.number().int(), street: z.string() }).nullable(),
  createdAt: z.iso.datetime(),
  lastAttemptAt: z.iso.datetime().nullable(),
  /** Since it was saved, or since its last retry */
  attempts: z.number().int(),
  lastError: z.string().nullable(),
  retriedAt: z.iso.datetime().nullable(),
  /** Who retried it last; null when nobody did, or the account no longer exists */
  retriedBy: z.string().nullable(),
});
export type FailedNotification = z.infer<typeof failedNotificationSchema>;

export const failedPageSchema = z.object({
  items: z.array(failedNotificationSchema),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalCount: z.number().int(),
});
export type FailedPage = z.infer<typeof failedPageSchema>;

export const FAILED_PAGE_SIZE = 20;

export const FAILED_KEY = ['notifications', 'failed'] as const;
export const failedPageKey = (page: number) => [...FAILED_KEY, page] as const;

/** One page of the failed emails, newest failure first. */
export const fetchFailed = (page: number, signal?: AbortSignal) =>
  request(`/notifications/failed?page=${page}&pageSize=${FAILED_PAGE_SIZE}`, { schema: failedPageSchema, signal });

/** Sends a failed email again, with another 24 hours; a 409 means it isn't failed any more. */
export const retryNotification = (id: string) => request(`/notifications/${id}/retry`, { method: 'POST' });

/** Sends every failed email again; the API answers how many. */
export const retryAllFailed = () =>
  request('/notifications/failed/retry', { method: 'POST', schema: z.object({ retried: z.number().int() }) });
