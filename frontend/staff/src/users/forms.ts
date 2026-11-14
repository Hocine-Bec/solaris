import { z } from 'zod';
import { ApiError } from '../api/errors';
import { EMAIL } from '../signin/form';

/*
 * The account forms' own checks, before anything is sent: each field filled, the email shaped like one. The password
 * rule is the API's (it answers 400 with what a password misses, shown under the field): the screens state it in the
 * field's help and don't repeat it. A generated password follows it by construction.
 */

const name = z.string().trim().min(1, 'Enter their name.');
// An empty email gets one message, not a second about its shape.
const email = z.string().trim().min(1, { message: 'Enter an email, such as name@example.com.', abort: true })
  .regex(EMAIL, 'Enter an email, such as name@example.com.');
const password = (message: string) => z.string().min(1, message);

export const technicianSchema = z.object({ name, email, password: password('Enter a password.') });
export type TechnicianValues = z.infer<typeof technicianSchema>;

export const editSchema = z.object({ name, email });
export type EditValues = z.infer<typeof editSchema>;

export const newPasswordSchema = z.object({ password: password('Enter a password.') });
export type NewPasswordValues = z.infer<typeof newPasswordSchema>;

export const changePasswordSchema = z.object({
  currentPassword: password('Enter your current password.'),
  newPassword: password('Enter a new password.'),
});
export type ChangePasswordValues = z.infer<typeof changePasswordSchema>;

/** What the password rule asks, under every field that sets one */
export const RULE_HELP = 'At least 8 characters, with an uppercase letter, a number and a special character.';

// Easy to read aloud and to copy by hand: no 0/O, 1/l/I.
const SETS = ['ABCDEFGHJKLMNPQRSTUVWXYZ', 'abcdefghijkmnpqrstuvwxyz', '23456789', '!#$%&*+?@'] as const;

/** A whole number from 0 to `max` - 1, uniform: values past the last whole multiple of `max` are drawn again. */
function randomBelow(max: number, random: (buffer: Uint32Array) => Uint32Array): number {
  const limit = Math.floor(0x1_0000_0000 / max) * max;
  const buffer = new Uint32Array(1);
  for (;;) {
    const value = random(buffer)[0];
    if (value < limit) return value % max;
  }
}

/**
 * A strong password, made in the browser: 14 characters, at least one of each kind the rule asks for, in a random
 * order. `random` is the browser's cryptographic generator; tests can pass their own.
 */
export function generatePassword(random: (buffer: Uint32Array) => Uint32Array = b => crypto.getRandomValues(b)): string {
  const all = SETS.join('');
  const pick = (set: string) => set[randomBelow(set.length, random)];
  const chars = [...SETS.map(pick), ...Array.from({ length: 10 }, () => pick(all))];
  for (let i = chars.length - 1; i > 0; i--) {
    const j = randomBelow(i + 1, random);
    [chars[i], chars[j]] = [chars[j], chars[i]];
  }
  return chars.join('');
}

/** What a failed save shows: messages under the fields the API named, or one notice for the whole form. */
export type FormErrors = { fields: Record<string, string>; notice: string | null };

/**
 * Turns a failed save into what the form shows. A 400 puts each field's messages under it (the API names fields in
 * the request's terms: `name`, `password`, `currentPassword`); a taken email goes under the email; anything else is
 * a notice, in the API's words when it sent some.
 */
export function formErrors(error: unknown, options: { fields: readonly string[]; emailTaken?: string }): FormErrors {
  if (!(error instanceof ApiError)) return { fields: {}, notice: 'Something went wrong in the app. Try again; if it happens again, report it.' };
  if (error.code === 'user.email_taken' && options.emailTaken) return { fields: { email: options.emailTaken }, notice: null };
  if (error.kind === 'invalid') {
    const fields = Object.fromEntries(Object.entries(error.fields)
      .filter(([field]) => options.fields.includes(field))
      .map(([field, messages]) => [field, messages.map(m => (m.endsWith('.') ? m : `${m}.`)).join(' ')]));
    if (Object.keys(fields).length > 0) return { fields, notice: null };
  }
  return { fields: {}, notice: error.message };
}
