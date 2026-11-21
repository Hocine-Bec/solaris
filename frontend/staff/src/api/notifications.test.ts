import { afterEach, describe, expect, it, vi } from 'vitest';
import { fetchFailed, retryAllFailed, retryNotification } from './notifications';

const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
const answer = (response: Response) => vi.fn<typeof fetch>(async () => response);

afterEach(() => { vi.unstubAllGlobals(); });

// A failed email as the API returns it (GET /api/notifications/failed)
const FAILED = {
  id: '01a1f8c2-4d1e-7b3a-9c51-6e0d2f4a8b17', type: 'LeadWelcome',
  to: { name: 'Maya Brooks', address: 'maya.brooks@example.com' }, lead: { id: 128, name: 'Maya Brooks' }, installation: null,
  createdAt: '2026-11-14T08:51:00.1234560Z', lastAttemptAt: '2026-11-15T08:51:00.2000000Z', attempts: 14,
  lastError: "The mail server smtp.gmail.com:587 can't be reached.", retriedAt: null, retriedBy: null,
};

describe('fetchFailed', () => {
  it('asks for a page of 20 and returns it checked', async () => {
    const fetchMock = answer(json(200, { items: [FAILED], page: 1, pageSize: 20, totalCount: 1 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(fetchFailed(1)).resolves.toEqual({ items: [FAILED], page: 1, pageSize: 20, totalCount: 1 });
    expect(fetchMock.mock.calls[0][0]).toBe('/api/notifications/failed?page=1&pageSize=20');
  });

  it('fails as unexpected for an email type the app does not know', async () => {
    vi.stubGlobal('fetch', answer(json(200, { items: [{ ...FAILED, type: 'InvoiceReminder' }], page: 1, pageSize: 20, totalCount: 1 })));
    await expect(fetchFailed(1)).rejects.toMatchObject({ kind: 'unexpected' });
  });
});

describe('retrying', () => {
  it('posts one retry to its address, and Retry all to the list', async () => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(json(200, { retried: 4 }));
    vi.stubGlobal('fetch', fetchMock);
    await retryNotification(FAILED.id);
    await expect(retryAllFailed()).resolves.toEqual({ retried: 4 });
    expect(fetchMock.mock.calls.map(([url, init]) => `${init?.method} ${String(url)}`)).toEqual([
      `POST /api/notifications/${FAILED.id}/retry`,
      'POST /api/notifications/failed/retry',
    ]);
  });

  it('gives an email that is no longer failed as a conflict with its code', async () => {
    vi.stubGlobal('fetch', answer(json(409, { status: 409, code: 'notification.not_failed', detail: "This email isn't waiting for a retry: it was already retried, or sent." })));
    await expect(retryNotification(FAILED.id)).rejects.toMatchObject({ kind: 'conflict', code: 'notification.not_failed' });
  });
});
