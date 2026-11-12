// Sends the request and turns the API's answer into what the form shows. Every error is a ProblemDetails (RFC 9457)
// with a code; a refused field comes back in "errors", keyed by the request's property names.
import { CONTACT } from '../content/contact';
import type { CreateLeadRequest } from './lead';
import type { FieldName } from './types';

export type Problem = { field?: FieldName; message: string };

export type Outcome =
  | { kind: 'sent' }
  /** 400: the answers the API refused, each with its field when there is one */
  | { kind: 'invalid'; problems: Problem[] }
  /** Another refusal, with its title and what to do */
  | { kind: 'refused'; title: string; message: string };

const FIELD_OF: Record<string, FieldName | undefined> = {
  firstname: 'firstName',
  lastname: 'lastName',
  email: 'email',
  phonenumber: 'phone',
  propertystreet: 'street',
  propertycity: 'city',
  propertystate: 'state',
  propertyzipcode: 'zip',
  propertylatitude: 'street',
  propertylongitude: 'street',
  propertytype: 'type',
  ispropertyowner: 'own',
  monthlybillrange: 'bill',
  besttimetocontact: 'time',
  notes: 'notes',
};

type ProblemBody = { code?: string; title?: string; detail?: string; errors?: Record<string, string[]> };

const asProblem = (body: unknown): ProblemBody => (body && typeof body === 'object' ? body as ProblemBody : {});

const NOTHING_LOST = 'nothing you entered is lost.';

/** What the form shows for an answer of the API (status and parsed body). */
export function interpret(status: number, body: unknown): Outcome {
  if (status >= 200 && status < 300) return { kind: 'sent' };
  const problem = asProblem(body);
  if (status === 400) {
    const problems = Object.entries(problem.errors ?? {}).flatMap(([key, messages]) =>
      messages.map(message => ({ field: FIELD_OF[key.toLowerCase()], message })));
    return { kind: 'invalid', problems: problems.length ? problems : [{ message: problem.detail ?? problem.title ?? 'The request was refused.' }] };
  }
  if (status === 409 && problem.code === 'address.already_exists') {
    return { kind: 'refused', title: 'This address already has a request', message: `We already have a quote request for this property's address. To add to it or change it, write to ${CONTACT.email}.` };
  }
  if (status === 429) {
    return { kind: 'refused', title: 'Too many requests from this connection', message: `Wait a minute, then send it again. Nothing you entered is lost.` };
  }
  if (status >= 500) {
    return { kind: 'refused', title: "We couldn't send your request", message: `Something went wrong on our side. Try again in a few minutes; ${NOTHING_LOST}` };
  }
  return { kind: 'refused', title: "We couldn't send your request", message: problem.detail ?? problem.title ?? `The server answered ${status}.` };
}

/** No answer at all: the server is down or the connection is lost. */
export const UNREACHABLE: Outcome = {
  kind: 'refused',
  title: "We couldn't send your request",
  message: `Our server didn't answer. Check your connection and try again; ${NOTHING_LOST}`,
};

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

export async function sendLead(request: CreateLeadRequest, send: typeof fetch = fetch): Promise<Outcome> {
  let response: Response;
  try {
    response = await send(`${API_BASE}/leads`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json, application/problem+json' },
      body: JSON.stringify(request),
    });
  } catch {
    return UNREACHABLE;
  }
  const body = await response.json().catch(() => null);
  return interpret(response.status, body);
}
