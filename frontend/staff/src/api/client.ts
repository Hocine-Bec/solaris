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
export async function request<T>(path: string, { method = 'GET', body, headers, signal, schema }: Options & { schema?: z.ZodType<T> } = {}): Promise<T | void> {
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
  if (!schema) return;

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
