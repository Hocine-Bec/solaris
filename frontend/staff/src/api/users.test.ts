import { afterEach, describe, expect, it, vi } from 'vitest';
import { createTechnician, deactivateUser, fetchUser, fetchUsers, reactivateUser, setPassword, updateUser } from './users';

const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
const answer = (response: Response) => vi.fn<typeof fetch>(async () => response);

afterEach(() => { vi.unstubAllGlobals(); });

// An account as the API returns it (GET /api/users/{id})
const LENA = {
  id: 3, name: 'Lena Park', email: 'lena.park@solaris.test', role: 'Technician', isActive: true,
  createdAt: '2026-11-14T09:12:04.5210000Z', lastLoginAt: null,
};

describe('fetchUsers', () => {
  it('asks for a page of 20 and returns it checked', async () => {
    const fetchMock = answer(json(200, { items: [LENA], page: 2, pageSize: 20, totalCount: 21 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(fetchUsers(2)).resolves.toEqual({ items: [LENA], page: 2, pageSize: 20, totalCount: 21 });
    expect(fetchMock.mock.calls[0][0]).toBe('/api/users?page=2&pageSize=20');
  });

  it('fails as unexpected for a role the app does not know', async () => {
    vi.stubGlobal('fetch', answer(json(200, { items: [{ ...LENA, role: 'Customer' }], page: 1, pageSize: 20, totalCount: 1 })));
    await expect(fetchUsers(1)).rejects.toMatchObject({ kind: 'unexpected' });
  });
});

describe('fetchUser', () => {
  it('returns the account, signed in or never', async () => {
    vi.stubGlobal('fetch', answer(json(200, { ...LENA, lastLoginAt: '2026-11-14T10:00:00Z' })));
    await expect(fetchUser(3)).resolves.toMatchObject({ id: 3, lastLoginAt: '2026-11-14T10:00:00Z' });
  });

  it('says not found for a 404', async () => {
    vi.stubGlobal('fetch', answer(json(404, { status: 404, code: 'user.not_found', detail: 'User with ID 9 not found.' })));
    await expect(fetchUser(9)).rejects.toMatchObject({ kind: 'notFound', code: 'user.not_found' });
  });
});

describe('the changes', () => {
  it('creates a technician with the name, email and password, and returns the account', async () => {
    const fetchMock = answer(json(201, LENA));
    vi.stubGlobal('fetch', fetchMock);
    await expect(createTechnician({ name: 'Lena Park', email: 'lena.park@solaris.test', password: 'Kq7#vLm2tR9p' })).resolves.toEqual(LENA);
    expect(fetchMock.mock.calls[0][0]).toBe('/api/users');
    expect(fetchMock.mock.calls[0][1]).toMatchObject({
      method: 'POST', body: '{"name":"Lena Park","email":"lena.park@solaris.test","password":"Kq7#vLm2tR9p"}',
    });
  });

  it('sends each change to its address', async () => {
    const fetchMock = vi.fn<typeof fetch>(async () => new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await updateUser(3, { name: 'Lena Park', email: 'lena@solaris.test' });
    await setPassword(3, 'Inverter#77');
    await deactivateUser(3);
    await reactivateUser(3);
    expect(fetchMock.mock.calls.map(([url, init]) => `${init?.method} ${String(url)} ${init?.body ?? ''}`.trim())).toEqual([
      'PUT /api/users/3 {"name":"Lena Park","email":"lena@solaris.test"}',
      'PUT /api/users/3/password {"password":"Inverter#77"}',
      'POST /api/users/3/deactivate',
      'POST /api/users/3/reactivate',
    ]);
  });

  it('shows a taken email as a conflict with its code', async () => {
    vi.stubGlobal('fetch', answer(json(409, { status: 409, code: 'user.email_taken', detail: 'An account already uses this email.' })));
    await expect(createTechnician({ name: 'Lena Park', email: 'rosa@solaris.test', password: 'x' }))
      .rejects.toMatchObject({ kind: 'conflict', code: 'user.email_taken', message: 'An account already uses this email.' });
  });
});
