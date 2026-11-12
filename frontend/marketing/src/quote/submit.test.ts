import { describe, expect, it, vi } from 'vitest';
import { buildLeadRequest } from './lead';
import { interpret, sendLead, UNREACHABLE } from './submit';
import { EMPTY_VALUES } from './types';

describe('interpret', () => {
  it('treats 201 Created as sent', () => {
    expect(interpret(201, { id: 7 })).toEqual({ kind: 'sent' });
  });

  it("shows the API's own messages for a 400, each linked to its field", () => {
    const body = {
      code: 'validation_failed',
      errors: {
        PropertyZipCode: ['Zip code must be in format 12345 or 12345-6789'],
        PropertyState: ['State must be the two-letter code of a US state or DC, such as TX'],
      },
    };
    expect(interpret(400, body)).toEqual({
      kind: 'invalid',
      problems: [
        { field: 'zip', message: 'Zip code must be in format 12345 or 12345-6789' },
        { field: 'state', message: 'State must be the two-letter code of a US state or DC, such as TX' },
      ],
    });
  });

  it('keeps a refused answer that has no field of its own, without a link', () => {
    expect(interpret(400, { errors: { PropertyCountry: ['Country is required'] } }))
      .toEqual({ kind: 'invalid', problems: [{ field: undefined, message: 'Country is required' }] });
  });

  it('shows the detail of a 400 that lists no fields', () => {
    expect(interpret(400, { code: 'validation_failed', detail: 'The body could not be read.' }))
      .toEqual({ kind: 'invalid', problems: [{ message: 'The body could not be read.' }] });
  });

  it('explains a request for an address that already has one (409)', () => {
    const outcome = interpret(409, { code: 'address.already_exists', detail: 'Address already exists.' });
    expect(outcome).toMatchObject({ kind: 'refused', title: 'This address already has a request' });
  });

  it('asks to wait after a 429', () => {
    expect(interpret(429, null)).toMatchObject({ kind: 'refused', title: 'Too many requests from this connection' });
  });

  it("says it's on our side after a 500, without the server's details", () => {
    const outcome = interpret(500, { code: 'internal_error', detail: 'NullReferenceException at …' });
    expect(outcome).toMatchObject({ kind: 'refused', title: "We couldn't send your request" });
    expect(JSON.stringify(outcome)).not.toContain('NullReferenceException');
  });

  it("shows another refusal's detail", () => {
    expect(interpret(404, { code: 'not_found', title: 'Not Found', detail: 'No such endpoint.' }))
      .toEqual({ kind: 'refused', title: "We couldn't send your request", message: 'No such endpoint.' });
  });
});

describe('sendLead', () => {
  it('posts the request as JSON to the leads endpoint', async () => {
    const send = vi.fn(async () => new Response(JSON.stringify({ id: 1 }), { status: 201 }));
    const request = buildLeadRequest({ ...EMPTY_VALUES, firstName: 'Jane' }, null);

    expect(await sendLead(request, send)).toEqual({ kind: 'sent' });

    const [url, init] = send.mock.calls[0] as unknown as [string, RequestInit];
    expect(url).toMatch(/\/leads$/);
    expect(init.method).toBe('POST');
    expect(new Headers(init.headers).get('Content-Type')).toBe('application/json');
    expect(JSON.parse(init.body as string)).toEqual(request);
  });

  it('says the server did not answer when the request never arrives', async () => {
    const send = vi.fn(async () => { throw new TypeError('Failed to fetch'); });
    expect(await sendLead(buildLeadRequest(EMPTY_VALUES, null), send)).toEqual(UNREACHABLE);
  });

  it('copes with an error answer that has no body', async () => {
    const send = vi.fn(async () => new Response(null, { status: 502 }));
    expect(await sendLead(buildLeadRequest(EMPTY_VALUES, null), send)).toMatchObject({ kind: 'refused' });
  });
});
