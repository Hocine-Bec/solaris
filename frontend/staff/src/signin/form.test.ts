import { describe, expect, it } from 'vitest';
import { ApiError, MESSAGES, toApiError } from '../api/errors';
import { noticeFor, signInSchema } from './form';

const messages = (value: unknown) => {
  const result = signInSchema.safeParse(value);
  return result.success ? [] : result.error.issues.map(i => `${i.path.join('.')}: ${i.message}`);
};

describe('signInSchema', () => {
  it('accepts an email and a password, trimming the email', () => {
    expect(signInSchema.parse({ email: '  rosa@solaris.example ', password: 'x' }))
      .toEqual({ email: 'rosa@solaris.example', password: 'x' });
  });

  it("checks nothing of the password but that it's there: the rule applies when a password is set", () => {
    expect(messages({ email: 'rosa@solaris.example', password: 'short' })).toEqual([]);
  });

  it('asks for both when both are empty', () => {
    expect(messages({ email: '', password: '' })).toEqual(['email: Enter your email.', 'password: Enter your password.']);
  });

  it.each(['rosa', 'rosa@', 'rosa@solaris', 'rosa @solaris.example'])('says %j is not an email address', email => {
    expect(messages({ email, password: 'x' })).toEqual(['email: Enter an email address, such as name@example.com.']);
  });
});

describe('noticeFor', () => {
  // The API's answer to wrong credentials, as recorded (POST /api/auth/login)
  const refused = toApiError(401, {
    title: 'Unauthorized', status: 401, detail: 'The email or password is incorrect.', code: 'auth.invalid_credentials',
  });

  it('shows one message for every refusal', () => {
    expect(noticeFor(refused)).toEqual({ kind: 'refused' });
  });

  it('says to wait after too many attempts, and to check the connection when nothing answered', () => {
    expect(noticeFor(new ApiError('tooMany', MESSAGES.tooMany, { status: 429 }))).toEqual({ kind: 'limited' });
    expect(noticeFor(new ApiError('unreachable', MESSAGES.unreachable))).toEqual({ kind: 'down' });
  });

  it("shows the error's own message for anything else", () => {
    expect(noticeFor(new ApiError('server', MESSAGES.server, { status: 500 }))).toEqual({ kind: 'error', text: MESSAGES.server });
  });

  it("explains an error from the app's own code without its details", () => {
    expect(noticeFor(new TypeError('x is undefined'))).toEqual({
      kind: 'error', text: 'Something went wrong in the app. Try again; if it happens again, report it.',
    });
  });
});
