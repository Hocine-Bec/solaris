import { MutationObserver } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';
import { SESSION_KEY } from '../session/session';
import { ApiError, MESSAGES } from './errors';
import { createQueryClient, shouldRetry } from './queryClient';

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

describe('a 401 anywhere', () => {
  const signedOut = new ApiError('signedOut', MESSAGES.signedOut, { status: 401 });
  const ROSA = { name: 'Rosa Castillo', email: 'rosa.castillo@solaris.test', role: 'Admin' };

  const signedInClient = () => {
    const client = createQueryClient();
    client.setQueryData(SESSION_KEY, ROSA);
    return client;
  };

  it('forgets the session when a load is refused', async () => {
    const client = signedInClient();
    await client.fetchQuery({ queryKey: ['leads'], queryFn: () => Promise.reject(signedOut) }).catch(() => {});
    expect(client.getQueryData(SESSION_KEY)).toBeNull();
  });

  it('forgets the session when a change is refused', async () => {
    const client = signedInClient();
    await new MutationObserver(client, { mutationFn: () => Promise.reject(signedOut) }).mutate().catch(() => {});
    expect(client.getQueryData(SESSION_KEY)).toBeNull();
  });

  it('keeps the session for any other failure', async () => {
    const client = signedInClient();
    await client.fetchQuery({ queryKey: ['leads'], queryFn: () => Promise.reject(error('notFound')), retry: false }).catch(() => {});
    expect(client.getQueryData(SESSION_KEY)).toEqual(ROSA);
  });
});
