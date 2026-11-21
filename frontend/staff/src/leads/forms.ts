import { z } from 'zod';
import { EMAIL } from '../signin/form';

/*
 * The Edit dialog's own checks, before anything is sent: the quote form's rules for the same fields, so the API rarely
 * has to refuse one. It still checks everything (a name that holds a web address, an email another open lead has).
 */

const name = (label: string) => z.string().trim()
  .min(1, { message: `Enter their ${label}.`, abort: true })
  .min(2, `The ${label} needs at least 2 characters.`)
  .max(100, `The ${label} can't be longer than 100 characters.`);

export const editLeadSchema = z.object({
  firstName: name('first name'),
  lastName: name('last name'),
  email: z.string().trim()
    .min(1, { message: 'Enter an email, such as name@example.com.', abort: true })
    .regex(EMAIL, 'Enter an email, such as name@example.com.')
    .max(255, "The email can't be longer than 255 characters."),
  phone: z.string().trim()
    .min(1, { message: 'Enter a phone number.', abort: true })
    .regex(/^\+?[\d\s\-()]+$/, 'Enter a phone number: digits, spaces, dashes and brackets, with + first if needed.')
    .max(20, "The phone number can't be longer than 20 characters."),
});
export type EditLeadValues = z.infer<typeof editLeadSchema>;
