import { z } from 'zod';
import { ROLES } from './auth';
import { request } from './client';

/** A staff account, as the API describes it (GET /api/users, GET /api/users/{id}); never its password. */
export const userSchema = z.object({
  id: z.number().int(),
  name: z.string(),
  email: z.string(),
  role: z.enum(ROLES),
  isActive: z.boolean(),
  createdAt: z.iso.datetime(),
  /** Null until the account first signs in */
  lastLoginAt: z.iso.datetime().nullable(),
});
export type User = z.infer<typeof userSchema>;

export const usersPageSchema = z.object({
  items: z.array(userSchema),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalCount: z.number().int(),
});
export type UsersPage = z.infer<typeof usersPageSchema>;

/** Accounts per page of the list; an office has a few, so one page usually holds them all. */
export const USERS_PAGE_SIZE = 20;

/** The accounts' places in TanStack Query's cache: every list page and every account start with it. */
export const USERS_KEY = ['users'] as const;
export const usersPageKey = (page: number) => [...USERS_KEY, 'page', page] as const;
export const userKey = (id: number) => [...USERS_KEY, id] as const;

/** One page of accounts: active ones first, then by name. */
export const fetchUsers = (page: number, signal?: AbortSignal) =>
  request(`/users?page=${page}&pageSize=${USERS_PAGE_SIZE}`, { schema: usersPageSchema, signal });

export const fetchUser = (id: number, signal?: AbortSignal) =>
  request(`/users/${id}`, { schema: userSchema, signal });

export type NewTechnician = { name: string; email: string; password: string };

/** A new technician's account; the API answers with it, without the password. */
export const createTechnician = (values: NewTechnician) =>
  request('/users', { method: 'POST', body: values, schema: userSchema });

/** A corrected name or a new email; the password and the sessions stay. */
export const updateUser = (id: number, values: { name: string; email: string }) =>
  request(`/users/${id}`, { method: 'PUT', body: values });

/** A new password for someone else's account; the API ends that account's sessions. */
export const setPassword = (id: number, password: string) =>
  request(`/users/${id}/password`, { method: 'PUT', body: { password } });

export const deactivateUser = (id: number) => request(`/users/${id}/deactivate`, { method: 'POST' });

export const reactivateUser = (id: number) => request(`/users/${id}/reactivate`, { method: 'POST' });

/** The active technicians, by name, for an installation's Assign dialog: an office has a few, so one page of 100. */
export async function fetchTechnicians(signal?: AbortSignal) {
  const page = await request('/users?page=1&pageSize=100', { schema: usersPageSchema, signal });
  return page.items.filter(u => u.role === 'Technician' && u.isActive);
}
export const TECHNICIANS_KEY = [...USERS_KEY, 'technicians'] as const;
