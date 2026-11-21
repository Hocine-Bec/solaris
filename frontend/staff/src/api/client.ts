import type { z } from 'zod';
import { ApiError, MESSAGES, toApiError } from './errors';

/**
 * The API, on the app's own address: the session travels in an HttpOnly cookie the browser sends by itself, so the
 * app never sees the token. In development, Vite's server passes /api on to the API (vite.config.ts).
 */
const BASE = '/api';

type Options = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  /** Sent as JSON */
  body?: unknown;
  headers?: Record<string, string>;
  /** Cancels the request (TanStack Query passes one, so leaving a screen cancels its loads) */
  signal?: AbortSignal;
};

/**
 * Sends a request and checks the answer: with a schema, the JSON it returns must match it, or the request fails as
 * `unexpected`; without one, the body is ignored. Every failure is an {@link ApiError}, except a cancelled request,
 * which rejects with the browser's own AbortError.
 */
export function request<T>(path: string, options: Options & { schema: z.ZodType<T> }): Promise<T>;
export function request(path: string, options?: Options): Promise<void>;
export async function request<T>(path: string, options: Options & { schema?: z.ZodType<T> } = {}): Promise<T | void> {
  const response = await send(path, options);
  if (!options.schema) return;
  return parse(response, options.schema);
}

/** An answer with the version of the record it describes: its `ETag`, null when the API sends none. */
export type Tagged<T> = { data: T; etag: string | null };

/**
 * As {@link request}, for a record changed with `If-Match` (BR-I4): sends `ifMatch` when there is one, and returns the
 * answer with its `ETag`, for the next change to send. An API that doesn't check versions sends none, and is sent none.
 */
export async function requestTagged<T>(path: string, { ifMatch, ...options }: Options & { schema: z.ZodType<T>; ifMatch?: string | null }): Promise<Tagged<T>> {
  const response = await send(path, { ...options, headers: { ...options.headers, ...(ifMatch && { 'If-Match': ifMatch }) } });
  return { data: await parse(response, options.schema), etag: response.headers.get('ETag') };
}

async function send(path: string, { method = 'GET', body, headers, signal }: Options): Promise<Response> {
  let response: Response;
  try {
    response = await fetch(BASE + path, {
      method,
      signal,
      headers: { Accept: 'application/json', ...(body !== undefined && { 'Content-Type': 'application/json' }), ...headers },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (error) {
    if (signal?.aborted) throw error;
    throw new ApiError('unreachable', MESSAGES.unreachable);
  }
  if (!response.ok) throw toApiError(response.status, await readJson(response));
  return response;
}

async function parse<T>(response: Response, schema: z.ZodType<T>): Promise<T> {
  const parsed = schema.safeParse(await readJson(response));
  if (!parsed.success) throw new ApiError('unexpected', MESSAGES.unexpected, { status: response.status });
  return parsed.data;
}

/** The body as JSON, or undefined when it is empty or isn't JSON */
async function readJson(response: Response): Promise<unknown> {
  try {
    return JSON.parse(await response.text());
  } catch {
    return undefined;
  }
}
