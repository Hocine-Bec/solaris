import { z } from 'zod';
import { ApiError } from '../api/errors';
import type { SignInReason } from '../session/session';

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * The sign-in form's own checks, before anything is sent: both fields filled, the email shaped like one. Whether
 * they are right is the API's answer, and the password rule applies when a password is set, not here.
 */
export const signInSchema = z.object({
  // An empty email gets one message, not a second about its shape.
  email: z.string().trim().min(1, { message: 'Enter your email.', abort: true }).regex(EMAIL, 'Enter an email address, such as name@example.com.'),
  password: z.string().min(1, 'Enter your password.'),
});
export type SignInValues = z.infer<typeof signInSchema>;

/** A message the card shows above the form */
export type Notice =
  | { kind: SignInReason }
  | { kind: 'refused' | 'limited' | 'down' }
  | { kind: 'error'; text: string };

/** What a failed sign-in shows: one message for every refusal, as the API sends one answer for all of them. */
export function noticeFor(error: unknown): Notice {
  if (!(error instanceof ApiError)) return { kind: 'error', text: 'Something went wrong in the app. Try again; if it happens again, report it.' };
  switch (error.kind) {
    case 'signedOut': return { kind: 'refused' };
    case 'tooMany': return { kind: 'limited' };
    case 'unreachable': return { kind: 'down' };
    default: return { kind: 'error', text: error.message };
  }
}
