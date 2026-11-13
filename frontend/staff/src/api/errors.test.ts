import { describe, expect, it } from 'vitest';
import { ApiError, MESSAGES, isStale, kindOf, toApiError } from './errors';

// Answers recorded from the API (POST /api/leads, GET /api/leads/{id}, an unknown URL, a wrong method)
const LEAD_NOT_FOUND = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.5', title: 'Not Found', status: 404,
  detail: 'Lead with ID 999999 not found.', traceId: '00-6c5907b8d2f576b38043a07eea73728d-3944f4a9c9f8a9b9-00', code: 'lead.not_found',
};
const UNKNOWN_URL = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.5', title: 'Not Found', status: 404,
  traceId: '00-b65f7c0ffc8c27e7ebf352ee96d33e13-414b099d84bc1190-00', code: 'not_found',
};
const METHOD_NOT_ALLOWED = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.6', title: 'Method Not Allowed', status: 405,
  traceId: '00-23ef0900956245b64ccabd4487c5e07e-32050c93871a3231-00', code: 'method_not_allowed',
};
const INVALID = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1', title: 'One or more validation errors occurred.', status: 400,
  errors: {
    FirstName: ['First name is required', 'First name must be at least 2 characters'],
    PropertyState: ['State must be the two-letter code of a US state or DC, such as TX'],
  },
  traceId: '00-ab09d53cbd39bb4b0d9d17f1e72f2ec5-dbf42183a0b920df-00', code: 'validation_failed',
};
const ADDRESS_EXISTS = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.10', title: 'Conflict', status: 409,
  detail: 'Address already exists.', traceId: '00-478aa8ebd7a58665ca028b0f03e6d17f-5ebfe7aaf7ff94f3-00', code: 'address.already_exists',
};
// The API's documented answer when a row changed after it was read (docs/architecture.md, error responses)
const CONCURRENT_CHANGE = { title: 'Conflict', status: 409, detail: 'This lead was changed by someone else.', code: 'lead.concurrent_change' };

describe('kindOf', () => {
  it.each([
    [400, 'invalid'], [401, 'signedOut'], [403, 'forbidden'], [404, 'notFound'], [409, 'conflict'], [429, 'tooMany'],
    [500, 'server'], [502, 'server'], [503, 'server'],
    [405, 'unexpected'], [415, 'unexpected'], [428, 'unexpected'],
  ] as const)('%i is %s', (status, kind) => {
    expect(kindOf(status)).toBe(kind);
  });
});

describe('toApiError', () => {
  it("shows the API's own message, with its code and trace id", () => {
    const error = toApiError(404, LEAD_NOT_FOUND);
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({
      kind: 'notFound', status: 404, code: 'lead.not_found', message: 'Lead with ID 999999 not found.',
      traceId: '00-6c5907b8d2f576b38043a07eea73728d-3944f4a9c9f8a9b9-00',
    });
  });

  it("uses the app's message when the API sends none", () => {
    expect(toApiError(404, UNKNOWN_URL)).toMatchObject({ kind: 'notFound', code: 'not_found', message: MESSAGES.notFound });
  });

  it('lists the refused fields by their JSON names', () => {
    const error = toApiError(400, INVALID);
    expect(error.kind).toBe('invalid');
    expect(error.message).toBe(MESSAGES.invalid);
    expect(error.fields).toEqual({
      firstName: ['First name is required', 'First name must be at least 2 characters'],
      propertyState: ['State must be the two-letter code of a US state or DC, such as TX'],
    });
  });

  it('names nested fields in camelCase too', () => {
    expect(toApiError(400, { errors: { 'Address.ZipCode': ['ZIP code is required'] } }).fields).toEqual({ 'address.zipCode': ['ZIP code is required'] });
  });

  it("keeps the API's message for a conflict", () => {
    expect(toApiError(409, ADDRESS_EXISTS)).toMatchObject({ kind: 'conflict', code: 'address.already_exists', message: 'Address already exists.' });
  });

  it('treats a status the app never expects as a bug, not as something the user did', () => {
    expect(toApiError(405, METHOD_NOT_ALLOWED)).toMatchObject({ kind: 'unexpected', code: 'method_not_allowed', message: MESSAGES.unexpected });
  });

  it("keeps the app's wording for a server error, whatever the body says", () => {
    expect(toApiError(500, { detail: 'NullReferenceException at …', code: 'internal_error' })).toMatchObject({ kind: 'server', message: MESSAGES.server, code: 'internal_error' });
  });

  it("copes with a body that isn't a ProblemDetails, such as a proxy's page", () => {
    const error = toApiError(502, undefined);
    expect(error).toMatchObject({ kind: 'server', status: 502, code: null, traceId: null, message: MESSAGES.server });
    expect(error.fields).toEqual({});
    expect(toApiError(400, { errors: 'not a map' })).toMatchObject({ kind: 'invalid', message: MESSAGES.invalid, fields: {} });
  });
});

describe('isStale', () => {
  it('is true for a row changed since it was read', () => {
    expect(isStale(toApiError(409, CONCURRENT_CHANGE))).toBe(true);
  });

  it('is false for the other conflicts and errors', () => {
    expect(isStale(toApiError(409, ADDRESS_EXISTS))).toBe(false);
    expect(isStale(toApiError(404, LEAD_NOT_FOUND))).toBe(false);
    expect(isStale(new Error('lead.concurrent_change'))).toBe(false);
  });
});
