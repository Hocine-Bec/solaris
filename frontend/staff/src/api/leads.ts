import { z } from 'zod';
import { request } from './client';

/** A lead's statuses (BR-L2): open while New or Contacted; Converted, Disqualified and Lost are final. */
export const LEAD_STATUSES = ['New', 'Contacted', 'Converted', 'Disqualified', 'Lost'] as const;
export type LeadStatus = (typeof LEAD_STATUSES)[number];

/** A US property's address: the street, then the city, state and ZIP code */
export const addressSchema = z.object({ street: z.string(), city: z.string(), state: z.string(), zipCode: z.string() });
export type Address = z.infer<typeof addressSchema>;

/** A lead in the list (GET /api/leads) */
export const leadSummarySchema = z.object({
  id: z.number().int(),
  name: z.string(),
  email: z.string(),
  address: addressSchema,
  propertyType: z.string(),
  status: z.enum(LEAD_STATUSES),
  createdAt: z.iso.datetime(),
});
export type LeadSummary = z.infer<typeof leadSummarySchema>;

export const leadsPageSchema = z.object({
  items: z.array(leadSummarySchema),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalCount: z.number().int(),
});
export type LeadsPage = z.infer<typeof leadsPageSchema>;

/**
 * One lead, as its page shows it (GET /api/leads/{id}, and every change's answer). `next` is what it can become now,
 * from the API's rules; `customer` is the one it was converted into or, while it is open, the customer that already
 * has its email (whom converting would add to); `proposedTimeZone`, while it is open, the zone converting would give
 * its installation.
 */
export const leadSchema = z.object({
  id: z.number().int(),
  firstName: z.string(),
  lastName: z.string(),
  name: z.string(),
  email: z.string(),
  phone: z.string(),
  bestTimeToContact: z.string(),
  address: addressSchema,
  propertyType: z.string(),
  isPropertyOwner: z.boolean(),
  monthlyBillRange: z.string(),
  notes: z.string().nullable(),
  status: z.enum(LEAD_STATUSES),
  createdAt: z.iso.datetime(),
  contactedAt: z.iso.datetime().nullable(),
  closedAt: z.iso.datetime().nullable(),
  next: z.array(z.enum(LEAD_STATUSES)),
  customer: z.object({ id: z.number().int(), name: z.string(), since: z.iso.datetime() }).nullable(),
  installationId: z.number().int().nullable(),
  proposedTimeZone: z.object({ id: z.string(), name: z.string() }).nullable(),
});
export type Lead = z.infer<typeof leadSchema>;

/** Leads per page of the list */
export const LEADS_PAGE_SIZE = 20;

/** The leads' places in TanStack Query's cache: every list page and every lead start with it. */
export const LEADS_KEY = ['leads'] as const;
export const leadsPageKey = (statuses: readonly LeadStatus[], page: number) => [...LEADS_KEY, 'page', statuses.join(','), page] as const;
export const leadKey = (id: number) => [...LEADS_KEY, id] as const;

/** One page of the leads in these statuses (all of them when none is given), newest first. */
export function fetchLeads(statuses: readonly LeadStatus[], page: number, signal?: AbortSignal) {
  const query = new URLSearchParams([...statuses.map(s => ['status', s]), ['page', String(page)], ['pageSize', String(LEADS_PAGE_SIZE)]]);
  return request(`/leads?${query}`, { schema: leadsPageSchema, signal });
}

export const fetchLead = (id: number, signal?: AbortSignal) => request(`/leads/${id}`, { schema: leadSchema, signal });

/** The moves the lead's page asks for by name; the API sets the time. Converted comes from {@link convertLead}. */
export type Move = 'Contacted' | 'Disqualified' | 'Lost';

export const moveLead = (id: number, status: Move) =>
  request(`/leads/${id}/status`, { method: 'PUT', body: { status }, schema: leadSchema });

/** A Contacted lead becomes a customer (or joins the one with its email) and an installation at Survey. */
export const convertLead = (id: number) => request(`/leads/${id}/convert`, { method: 'POST', schema: leadSchema });

export type LeadContact = { firstName: string; lastName: string; email: string; phone: string };

/** An open lead's corrected name, email and phone (FR-L7) */
export const updateLead = (id: number, values: LeadContact) =>
  request(`/leads/${id}`, { method: 'PUT', body: values, schema: leadSchema });
