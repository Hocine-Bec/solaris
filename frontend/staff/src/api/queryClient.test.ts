import { describe, expect, it } from 'vitest';
import { ApiError, MESSAGES } from './errors';
import { shouldRetry } from './queryClient';

const error = (kind: ApiError['kind']) => new ApiError(kind, MESSAGES[kind]);

describe('shouldRetry', () => {
  it('tries a load once more when the network or the server failed', () => {
    expect(shouldRetry(0, error('unreachable'))).toBe(true);
    expect(shouldRetry(0, error('server'))).toBe(true);
  });

  it('stops after that one retry', () => {
    expect(shouldRetry(1, error('unreachable'))).toBe(false);
    expect(shouldRetry(1, error('server'))).toBe(false);
  });

  it.each(['invalid', 'signedOut', 'forbidden', 'notFound', 'conflict', 'tooMany', 'unexpected'] as const)("doesn't retry %s: the answer is final", kind => {
    expect(shouldRetry(0, error(kind))).toBe(false);
  });

  it("doesn't retry an error from the app's own code", () => {
    expect(shouldRetry(0, new TypeError('x is undefined'))).toBe(false);
  });
});
