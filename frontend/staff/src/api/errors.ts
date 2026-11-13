import { z } from 'zod';

/**
 * What went wrong with a request, in the terms a screen acts on. `unexpected` means the client and the API disagree
 * (an answer the app doesn't understand, or a status it never expects): a bug, not something the user caused.
 */
export type ErrorKind =
  | 'unreachable' | 'invalid' | 'signedOut' | 'forbidden' | 'notFound' | 'conflict' | 'tooMany' | 'server' | 'unexpected';

/** A failed request: its kind, the message to show, and the API's own details when it sent them. */
export class ApiError extends Error {
  readonly kind: ErrorKind;
  /** The HTTP status; null when no answer came */
  readonly status: number | null;
  /** The API's stable code, such as `lead.not_found` */
  readonly code: string | null;
  /** The messages for each field the API refused, by field name in camelCase (`propertyState`, `address.city`) */
  readonly fields: Readonly<Record<string, string[]>>;
  /** The id the API logged the request under */
  readonly traceId: string | null;

  constructor(kind: ErrorKind, message: string, details: { status?: number | null; code?: string | null; fields?: Record<string, string[]>; traceId?: string | null } = {}) {
    super(message);
    this.name = 'ApiError';
    this.kind = kind;
    this.status = details.status ?? null;
    this.code = details.code ?? null;
    this.fields = details.fields ?? {};
    this.traceId = details.traceId ?? null;
  }
}

/** The messages for each kind, when the API's answer has none of its own */
export const MESSAGES: Record<ErrorKind, string> = {
  unreachable: "The server didn't answer. Check your connection, then try again.",
  invalid: 'Some of the answers need fixing.',
  signedOut: 'Your session ended. Sign in again to go back to the page you were on.',
  forbidden: "Your account doesn't have access to this.",
  notFound: "This doesn't exist. The link may be wrong, or it was removed.",
  conflict: 'Someone else changed this. Reload to see the latest version, then make your change again.',
  tooMany: 'Too many requests. Wait a minute, then try again.',
  server: 'Something went wrong on the server. Try again in a moment.',
  unexpected: "The server's answer wasn't what the app expected. Try again; if it happens again, report it.",
};

/** A gateway's 502, 503 or 504 means the API behind it didn't answer: it isn't running, or can't be reached. */
export function kindOf(status: number): ErrorKind {
  switch (status) {
    case 400: return 'invalid';
    case 401: return 'signedOut';
    case 403: return 'forbidden';
    case 404: return 'notFound';
    case 409: return 'conflict';
    case 429: return 'tooMany';
    case 502: case 503: case 504: return 'unreachable';
    default: return status >= 500 ? 'server' : 'unexpected';
  }
}

/** An error response from the API: a ProblemDetails (RFC 9457) with a stable `code` and a `traceId` */
const problemSchema = z.object({
  detail: z.string().optional(),
  code: z.string().optional(),
  traceId: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
});

/** `PropertyState` → `propertyState`, `Address.City` → `address.city`, as the API's JSON names them */
const camel = (name: string) => name.split('.').map(part => part.charAt(0).toLowerCase() + part.slice(1)).join('.');

/**
 * The error for a response that isn't a success, from its status and its body (anything: a ProblemDetails, or a
 * proxy's HTML page). The API writes its `detail` for people, so it is the message when there is one; the kinds whose
 * meaning doesn't depend on the case (signed out, too many requests, a server error) keep the app's own wording.
 */
export function toApiError(status: number, body: unknown): ApiError {
  const kind = kindOf(status);
  const parsed = problemSchema.safeParse(body);
  const problem = parsed.success ? parsed.data : {};
  const own = kind === 'invalid' || kind === 'notFound' || kind === 'conflict' || kind === 'forbidden';
  const fields = Object.fromEntries(Object.entries(problem.errors ?? {}).map(([name, messages]) => [camel(name), messages]));
  return new ApiError(kind, (own && problem.detail) || MESSAGES[kind], { status, code: problem.code, fields, traceId: problem.traceId });
}

/** A 409 because the record changed since it was read (BR-I4): the screen keeps what it shows and asks to reload. */
export const isStale = (error: unknown): error is ApiError =>
  error instanceof ApiError && error.kind === 'conflict' && !!error.code?.endsWith('.concurrent_change');
