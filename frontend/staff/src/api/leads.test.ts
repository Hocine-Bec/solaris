import { afterEach, describe, expect, it, vi } from 'vitest';
import { convertLead, fetchLead, fetchLeads, moveLead, updateLead } from './leads';
import { fetchCustomer, fetchCustomers } from './customers';

const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
const answer = (response: Response) => vi.fn<typeof fetch>(async () => response);
const sent = (fetchMock: ReturnType<typeof vi.fn<typeof fetch>>) =>
  fetchMock.mock.calls.map(([url, init]) => `${init?.method ?? 'GET'} ${String(url)} ${init?.body ?? ''}`.trim());

afterEach(() => { vi.unstubAllGlobals(); });

const ADDRESS = { street: '2104 Oak Hollow Dr', city: 'Round Rock', state: 'TX', zipCode: '78681' };

// A lead as the API returns it (GET /api/leads/{id})
const DANA = {
  id: 130, firstName: 'Dana', lastName: 'Whitfield', name: 'Dana Whitfield', email: 'dana.whitfield@example.com',
  phone: '(512) 555-0187', bestTimeToContact: 'Afternoon', address: ADDRESS, propertyType: 'House', isPropertyOwner: true,
  monthlyBillRange: '100-200', notes: null, status: 'New', createdAt: '2026-11-20T07:12:00Z', contactedAt: null,
  closedAt: null, next: ['Contacted', 'Disqualified', 'Lost'], customer: null, installationId: null,
  proposedTimeZone: { id: 'America/Chicago', name: 'Central' },
};

describe('fetchLeads', () => {
  it("asks for each of the filter's statuses, and a page of 20", async () => {
    const fetchMock = vi.fn<typeof fetch>(async () => json(200, { items: [], page: 1, pageSize: 20, totalCount: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await fetchLeads(['Converted', 'Disqualified', 'Lost'], 2);
    await fetchLeads([], 1);
    expect(sent(fetchMock)).toEqual([
      'GET /api/leads?status=Converted&status=Disqualified&status=Lost&page=2&pageSize=20',
      'GET /api/leads?page=1&pageSize=20',
    ]);
  });

  it('fails as unexpected for a status the app does not know', async () => {
    const item = { id: 1, name: 'Dana Whitfield', email: 'd@example.com', address: ADDRESS, propertyType: 'House', status: 'Qualified', createdAt: '2026-11-20T07:12:00Z' };
    vi.stubGlobal('fetch', answer(json(200, { items: [item], page: 1, pageSize: 20, totalCount: 1 })));
    await expect(fetchLeads([], 1)).rejects.toMatchObject({ kind: 'unexpected' });
  });
});

describe('a lead', () => {
  it('returns it checked', async () => {
    vi.stubGlobal('fetch', answer(json(200, DANA)));
    await expect(fetchLead(130)).resolves.toEqual(DANA);
  });

  it('sends each change to its address, and returns the lead as the API answers it', async () => {
    const fetchMock = vi.fn<typeof fetch>(async () => json(200, DANA));
    vi.stubGlobal('fetch', fetchMock);
    await expect(moveLead(130, 'Contacted')).resolves.toEqual(DANA);
    await convertLead(130);
    await updateLead(130, { firstName: 'Dana', lastName: 'Whitfield', email: 'dana@example.com', phone: '(512) 555-0187' });
    expect(sent(fetchMock)).toEqual([
      'PUT /api/leads/130/status {"status":"Contacted"}',
      'POST /api/leads/130/convert',
      'PUT /api/leads/130 {"firstName":"Dana","lastName":"Whitfield","email":"dana@example.com","phone":"(512) 555-0187"}',
    ]);
  });

  it('keeps the code of a move another Admin made first', async () => {
    vi.stubGlobal('fetch', answer(json(409, { code: 'lead.transition_not_allowed', detail: "This lead is Lost: it can't become Contacted." })));
    await expect(moveLead(130, 'Contacted')).rejects.toMatchObject({
      kind: 'conflict', code: 'lead.transition_not_allowed', message: "This lead is Lost: it can't become Contacted.",
    });
  });
});

describe('customers', () => {
  it('sends the search only when there is one', async () => {
    const fetchMock = vi.fn<typeof fetch>(async () => json(200, { items: [], page: 1, pageSize: 20, totalCount: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await fetchCustomers('', 1);
    await fetchCustomers('ra man', 3);
    expect(sent(fetchMock)).toEqual(['GET /api/customers?page=1&pageSize=20', 'GET /api/customers?q=ra+man&page=3&pageSize=20']);
  });

  it('returns a customer with their installations, checked', async () => {
    const priya = {
      id: 1, firstName: 'Priya', lastName: 'Raman', name: 'Priya Raman', email: 'priya.raman@example.com', phone: '(512) 555-0164',
      address: ADDRESS, status: 'Prospect', createdAt: '2026-11-18T10:05:00Z',
      installations: [{ id: 41, address: ADDRESS, status: 'Survey', createdAt: '2026-11-18T10:05:00Z', leadId: 128, leadReceivedAt: '2026-11-17T11:30:00Z' }],
    };
    vi.stubGlobal('fetch', answer(json(200, priya)));
    await expect(fetchCustomer(1)).resolves.toEqual(priya);
  });
});
