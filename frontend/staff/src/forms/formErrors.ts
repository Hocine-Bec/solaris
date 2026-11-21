import { ApiError } from '../api/errors';

/** What a failed save shows: messages under the fields the API named, or one notice for the whole form. */
export type FormErrors = { fields: Record<string, string>; notice: string | null };

/**
 * Turns a failed save into what the form shows. A 400 puts each field's messages under it (the API names fields in
 * the request's terms: `name`, `password`, `currentPassword`); a taken email (`user.email_taken`, `lead.email_taken`)
 * goes under the email; anything else is a notice, in the API's words when it sent some.
 */
export function formErrors(error: unknown, options: { fields: readonly string[]; emailTaken?: string }): FormErrors {
  if (!(error instanceof ApiError)) return { fields: {}, notice: 'Something went wrong in the app. Try again; if it happens again, report it.' };
  if (error.code?.endsWith('.email_taken') && options.emailTaken) return { fields: { email: options.emailTaken }, notice: null };
  if (error.kind === 'invalid') {
    const fields = Object.fromEntries(Object.entries(error.fields)
      .filter(([field]) => options.fields.includes(field))
      .map(([field, messages]) => [field, messages.map(m => (m.endsWith('.') ? m : `${m}.`)).join(' ')]));
    if (Object.keys(fields).length > 0) return { fields, notice: null };
  }
  return { fields: {}, notice: error.message };
}
