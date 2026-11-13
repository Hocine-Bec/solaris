import { afterEach, describe, expect, it, vi } from 'vitest';
import { z } from 'zod';
import { request } from './client';
import { ApiError, MESSAGES } from './errors';

const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
const answer = (response: Response | Error) => vi.fn<typeof fetch>(async () => { if (response instanceof Error) throw response; return response; });

afterEach(() => { vi.unstubAllGlobals(); });

const lead = z.object({ id: z.number(), fullName: z.string(), status: z.string() });

describe('request', () => {
  it("calls /api on the app's own address and returns the checked answer", async () => {
    const fetchMock = answer(json(200, { id: 1, fullName: 'Dana Whitfield', status: 'New', notes: null }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(request('/leads/1', { schema: lead })).resolves.toEqual({ id: 1, fullName: 'Dana Whitfield', status: 'New' });
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('/api/leads/1');
    expect(init).toMatchObject({ method: 'GET', body: undefined, headers: { Accept: 'application/json' } });
  });

  it('sends a body as JSON', async () => {
    const fetchMock = answer(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    await expect(request('/leads/1/status', { method: 'PUT', body: { status: 'Contacted' } })).resolves.toBeUndefined();
    expect(fetchMock.mock.calls[0][1]).toMatchObject({
      method: 'PUT', body: '{"status":"Contacted"}', headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
    });
  });

  it("fails as unexpected when the answer doesn't match the schema", async () => {
    vi.stubGlobal('fetch', answer(json(200, { id: '1' })));
    await expect(request('/leads/1', { schema: lead })).rejects.toMatchObject({ kind: 'unexpected', status: 200, message: MESSAGES.unexpected });
  });

  it("fails as unexpected when the answer isn't JSON", async () => {
    vi.stubGlobal('fetch', answer(new Response('<html>', { status: 200 })));
    await expect(request('/leads/1', { schema: lead })).rejects.toMatchObject({ kind: 'unexpected' });
  });

  it("turns an error answer into an ApiError with the API's details", async () => {
    vi.stubGlobal('fetch', answer(json(409, { status: 409, detail: 'Address already exists.', code: 'address.already_exists', traceId: '00-1' })));
    const error = await request('/leads', { method: 'POST', body: {} }).catch(e => e);
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ kind: 'conflict', status: 409, code: 'address.already_exists', message: 'Address already exists.', traceId: '00-1' });
  });

  it("copes with an error answer that isn't JSON", async () => {
    vi.stubGlobal('fetch', answer(new Response('Internal Server Error', { status: 500 })));
    await expect(request('/leads')).rejects.toMatchObject({ kind: 'server', status: 500, message: MESSAGES.server });
  });

  it("says the server didn't answer when the development server can't reach the API (its 502)", async () => {
    vi.stubGlobal('fetch', answer(new Response("The API isn't answering at http://localhost:5228.", { status: 502 })));
    await expect(request('/auth/me')).rejects.toMatchObject({ kind: 'unreachable', status: 502, message: MESSAGES.unreachable });
  });

  it('fails as unreachable when no answer comes', async () => {
    vi.stubGlobal('fetch', answer(new TypeError('Failed to fetch')));
    await expect(request('/leads')).rejects.toMatchObject({ kind: 'unreachable', status: null, message: MESSAGES.unreachable });
  });

  it("passes a cancellation on as the browser's own error", async () => {
    const controller = new AbortController();
    controller.abort();
    const aborted = new DOMException('The operation was aborted.', 'AbortError');
    vi.stubGlobal('fetch', answer(aborted));
    await expect(request('/leads', { signal: controller.signal })).rejects.toBe(aborted);
  });
});
