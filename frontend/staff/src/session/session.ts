import { createContext, useContext } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fetchCurrentUser, type CurrentUser, type Role } from '../api/auth';

/** The session's place in TanStack Query's cache: the signed-in account, or null */
export const SESSION_KEY = ['session'] as const;

/** Who is signed in. The API decides; the app asks when it opens, and again when its window comes back into focus. */
export const useSession = () =>
  useQuery({ queryKey: SESSION_KEY, queryFn: ({ signal }) => fetchCurrentUser(signal) });

/** Why the sign-in screen opened, when it says so */
export type SignInReason = 'ended' | 'out';

/**
 * Why the session is about to be forgotten, when it is on purpose: signing out sets it, then forgets the session, and
 * the session check opens the sign-in page with it. Without it, a session that disappears had ended.
 */
export const REASON_KEY = ['sign-in-reason'] as const;

export const CurrentUserContext = createContext<CurrentUser | null>(null);

/** The signed-in account, inside the screens behind the session check */
export function useCurrentUser(): CurrentUser {
  const user = useContext(CurrentUserContext);
  if (!user) throw new Error('useCurrentUser is used outside the screens behind the session check.');
  return user;
}

/** Where an account lands after signing in: the office on its new leads, a technician on their own work. */
export const landingFor = (role: Role) => (role === 'Admin' ? '/leads' : '/my-work');

/**
 * The page to go back to after signing in, if it is safe: an address inside the app (one leading slash, so never
 * another site: "//elsewhere.example" or "/\elsewhere.example" would be), and not the sign-in page itself.
 */
export function safeReturnTo(value: string | null): string | null {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) return null;
  if (value === '/login' || value.startsWith('/login?') || value.startsWith('/login/')) return null;
  return value;
}

/** The sign-in page's address, remembering the page to come back to */
export const loginPath = (returnTo: string) =>
  returnTo === '/' ? '/login' : `/login?returnTo=${encodeURIComponent(returnTo)}`;

/** "Rosa Castillo" → "RC", "Cher" → "C": the first letters of the first and last words */
export function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return '?';
  const first = [...words[0]][0];
  const last = words.length > 1 ? [...words[words.length - 1]][0] : '';
  return (first + last).toUpperCase();
}
