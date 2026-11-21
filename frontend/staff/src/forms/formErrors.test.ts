import { describe, expect, it } from 'vitest';
import { ApiError, MESSAGES } from '../api/errors';
import { formErrors } from './formErrors';

describe('formErrors', () => {
  const fields = ['name', 'email', 'password'] as const;

  it('puts what the API refused under each field it named, as sentences', () => {
    const error = new ApiError('invalid', 'One or more validation errors occurred.', {
      status: 400, code: 'validation_failed',
      fields: { password: ['Password must contain an uppercase letter', 'Password must contain a number'] },
    });
    expect(formErrors(error, { fields: [...fields] })).toEqual({
      fields: { password: 'Password must contain an uppercase letter. Password must contain a number.' }, notice: null,
    });
  });

  it("puts a taken email under the email, in the form's words, an account's or a lead's", () => {
    const error = new ApiError('conflict', 'An account already uses this email.', { status: 409, code: 'user.email_taken' });
    expect(formErrors(error, { fields: [...fields], emailTaken: 'Another account uses this email.' }))
      .toEqual({ fields: { email: 'Another account uses this email.' }, notice: null });
    const lead = new ApiError('conflict', 'Another open lead has this email.', { status: 409, code: 'lead.email_taken' });
    expect(formErrors(lead, { fields: [...fields], emailTaken: 'Another open lead has this email.' }))
      .toEqual({ fields: { email: 'Another open lead has this email.' }, notice: null });
  });

  it('shows anything else as one notice', () => {
    const unreachable = new ApiError('unreachable', MESSAGES.unreachable);
    expect(formErrors(unreachable, { fields: [...fields] })).toEqual({ fields: {}, notice: MESSAGES.unreachable });
    const notFound = new ApiError('notFound', 'User with ID 9 not found.', { status: 404, code: 'user.not_found' });
    expect(formErrors(notFound, { fields: [...fields] })).toEqual({ fields: {}, notice: 'User with ID 9 not found.' });
    // A field this form doesn't have can't be shown under it
    const other = new ApiError('invalid', 'One or more validation errors occurred.', { status: 400, fields: { role: ['Role is not allowed'] } });
    expect(formErrors(other, { fields: [...fields] })).toEqual({ fields: {}, notice: 'One or more validation errors occurred.' });
  });
});
