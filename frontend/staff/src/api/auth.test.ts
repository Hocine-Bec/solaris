import { afterEach, describe, expect, it, vi } from 'vitest';
import { fetchCurrentUser, signIn } from './auth';

const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
const answer = (response: Response) => vi.fn<typeof fetch>(async () => response);

afterEach(() => { vi.unstubAllGlobals(); });

// The API's answer for the first Admin (GET /api/auth/me, POST /api/auth/login)
const ROSA = { name: 'Rosa Castillo', email: 'rosa.castillo@solaris.test', role: 'Admin' };

describe('signIn', () => {
  it('posts the email and password and returns the account', async () => {
    const fetchMock = answer(json(200, ROSA));
    vi.stubGlobal('fetch', fetchMock);
    await expect(signIn('rosa.castillo@solaris.test', 'Solaris#2026')).resolves.toEqual(ROSA);
    expect(fetchMock.mock.calls[0][0]).toBe('/api/auth/login');
    expect(fetchMock.mock.calls[0][1]).toMatchObject({ method: 'POST', body: '{"email":"rosa.castillo@solaris.test","password":"Solaris#2026"}' });
  });

  it('fails as unexpected for a role the app does not know', async () => {
    vi.stubGlobal('fetch', answer(json(200, { ...ROSA, role: 'Customer' })));
    await expect(signIn('a@b.c', 'x')).rejects.toMatchObject({ kind: 'unexpected' });
  });
});

describe('fetchCurrentUser', () => {
  it('returns the account the session belongs to', async () => {
    vi.stubGlobal('fetch', answer(json(200, ROSA)));
    await expect(fetchCurrentUser()).resolves.toEqual(ROSA);
  });

  it('returns null for a 401: never signed in, ended, or deactivated', async () => {
    vi.stubGlobal('fetch', answer(json(401, { status: 401, code: 'auth.signed_out', detail: 'Your session has ended. Sign in again.' })));
    await expect(fetchCurrentUser()).resolves.toBeNull();
  });

  it('throws anything else, which says nothing about the session', async () => {
    vi.stubGlobal('fetch', answer(new Response('Internal Server Error', { status: 500 })));
    await expect(fetchCurrentUser()).rejects.toMatchObject({ kind: 'server' });
  });
});
