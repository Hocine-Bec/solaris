import { describe, expect, it } from 'vitest';
import { ApiError, MESSAGES } from '../api/errors';
import { changePasswordSchema, editSchema, formErrors, generatePassword, technicianSchema } from './forms';

const messages = (result: { success: boolean; error?: { issues: { path: PropertyKey[]; message: string }[] } }) =>
  result.success ? [] : result.error!.issues.map(i => `${String(i.path[0])}: ${i.message}`);

describe('the account forms', () => {
  it('sends the name and email trimmed, and the password as typed', () => {
    expect(technicianSchema.parse({ name: '  Lena Park ', email: ' lena.park@solaris.test ', password: ' Kq7#vLm2tR9p ' }))
      .toEqual({ name: 'Lena Park', email: 'lena.park@solaris.test', password: ' Kq7#vLm2tR9p ' });
  });

  it('asks for each empty field, and for an email shaped like one', () => {
    expect(messages(technicianSchema.safeParse({ name: ' ', email: '', password: '' }))).toEqual([
      'name: Enter their name.', 'email: Enter an email, such as name@example.com.', 'password: Enter a password.',
    ]);
    expect(messages(editSchema.safeParse({ name: 'Lena Park', email: 'lena.park' }))).toEqual([
      'email: Enter an email, such as name@example.com.',
    ]);
    expect(messages(changePasswordSchema.safeParse({ currentPassword: '', newPassword: '' }))).toEqual([
      'currentPassword: Enter your current password.', 'newPassword: Enter a new password.',
    ]);
  });

  it("leaves the password rule to the API, which answers with what a password misses", () => {
    expect(technicianSchema.safeParse({ name: 'Lena Park', email: 'lena@solaris.test', password: 'short' }).success).toBe(true);
  });
});

describe('generatePassword', () => {
  it('makes 14 characters with an uppercase letter, a lowercase one, a number and a special character', () => {
    for (let i = 0; i < 200; i++) {
      const password = generatePassword();
      expect(password).toHaveLength(14);
      expect(password).toMatch(/[A-Z]/);
      expect(password).toMatch(/[a-z]/);
      expect(password).toMatch(/[2-9]/);
      expect(password).toMatch(/[!#$%&*+?@]/);
      expect(password).not.toMatch(/[0O1lI\s]/);
    }
  });

  it('draws again rather than favour some characters: a value past the last whole multiple is thrown away', () => {
    // The first draw (0xFFFFFFFF) is past the last multiple of every set's size, so it is drawn again
    const draws = [0xFFFF_FFFF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    let n = 0;
    const password = generatePassword(buffer => { buffer[0] = draws[n++] ?? 0; return buffer; });
    expect(n).toBe(28);   // 14 picks and 13 swaps, plus the one value thrown away
    expect(password).toHaveLength(14);
  });
});

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

  it("puts a taken email under the email, in the form's words", () => {
    const error = new ApiError('conflict', 'An account already uses this email.', { status: 409, code: 'user.email_taken' });
    expect(formErrors(error, { fields: [...fields], emailTaken: 'Another account uses this email.' }))
      .toEqual({ fields: { email: 'Another account uses this email.' }, notice: null });
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
