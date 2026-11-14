import { z } from 'zod';
import { request } from './client';
import { ApiError } from './errors';

export const ROLES = ['Admin', 'Technician'] as const;
export type Role = (typeof ROLES)[number];

/** Who is signed in, as the API describes the account (GET /api/auth/me, and the answer to signing in) */
export const currentUserSchema = z.object({
  id: z.number().int(),
  name: z.string(),
  email: z.string(),
  role: z.enum(ROLES),
});
export type CurrentUser = z.infer<typeof currentUserSchema>;

/** Checks the email and password; the API answers with the account and sets the session cookie. */
export const signIn = (email: string, password: string) =>
  request('/auth/login', { method: 'POST', body: { email, password }, schema: currentUserSchema });

/**
 * Changes your own password, given the current one. The API ends your other sessions and sets a new session cookie
 * for this one; a wrong current password is a 400 on `currentPassword`, so the session goes on.
 */
export const changePassword = (currentPassword: string, newPassword: string) =>
  request('/auth/password', { method: 'PUT', body: { currentPassword, newPassword } });

/** Clears the session cookie; the API answers 204 even when the session had already ended. */
export const signOut = () => request('/auth/logout', { method: 'POST' });

/**
 * The account the session belongs to, or null when there is none: never signed in, ended, or deactivated since (a
 * 401). Any other failure (no answer, a server error) is thrown, as it says nothing about the session.
 */
export async function fetchCurrentUser(signal?: AbortSignal): Promise<CurrentUser | null> {
  try {
    return await request('/auth/me', { schema: currentUserSchema, signal });
  } catch (error) {
    if (error instanceof ApiError && error.kind === 'signedOut') return null;
    throw error;
  }
}
