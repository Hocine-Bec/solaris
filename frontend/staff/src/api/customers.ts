import { z } from 'zod';
import { request } from './client';
import { addressSchema } from './leads';

export const CUSTOMER_STATUSES = ['Prospect', 'Active', 'Inactive'] as const;
export const INSTALLATION_STATUSES = ['Survey', 'Design', 'Permits', 'Installation', 'Inspection', 'Active', 'Cancelled', 'Deactivated'] as const;
export type CustomerStatus = (typeof CUSTOMER_STATUSES)[number];
export type InstallationStatus = (typeof INSTALLATION_STATUSES)[number];

/** A customer in the list (GET /api/customers) */
export const customerSummarySchema = z.object({
  id: z.number().int(),
  name: z.string(),
  email: z.string(),
  phone: z.string(),
  status: z.enum(CUSTOMER_STATUSES),
  installationCount: z.number().int(),
  createdAt: z.iso.datetime(),
});
export type CustomerSummary = z.infer<typeof customerSummarySchema>;

export const customersPageSchema = z.object({
  items: z.array(customerSummarySchema),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalCount: z.number().int(),
});
export type CustomersPage = z.infer<typeof customersPageSchema>;

/** One customer with their installations, newest first, each with the lead it came from (GET /api/customers/{id}) */
export const customerSchema = z.object({
  id: z.number().int(),
  firstName: z.string(),
  lastName: z.string(),
  name: z.string(),
  email: z.string(),
  phone: z.string(),
  address: addressSchema.nullable(),
  status: z.enum(CUSTOMER_STATUSES),
  createdAt: z.iso.datetime(),
  installations: z.array(z.object({
    id: z.number().int(),
    address: addressSchema,
    status: z.enum(INSTALLATION_STATUSES),
    createdAt: z.iso.datetime(),
    leadId: z.number().int(),
    leadReceivedAt: z.iso.datetime(),
  })),
});
export type Customer = z.infer<typeof customerSchema>;

export const CUSTOMERS_PAGE_SIZE = 20;

export const CUSTOMERS_KEY = ['customers'] as const;
export const customersPageKey = (q: string, page: number) => [...CUSTOMERS_KEY, 'page', q, page] as const;
export const customerKey = (id: number) => [...CUSTOMERS_KEY, id] as const;

/** One page of customers whose name or email contains `q` (all of them when it is empty), newest first. */
export function fetchCustomers(q: string, page: number, signal?: AbortSignal) {
  const query = new URLSearchParams({ ...(q && { q }), page: String(page), pageSize: String(CUSTOMERS_PAGE_SIZE) });
  return request(`/customers?${query}`, { schema: customersPageSchema, signal });
}

export const fetchCustomer = (id: number, signal?: AbortSignal) => request(`/customers/${id}`, { schema: customerSchema, signal });
