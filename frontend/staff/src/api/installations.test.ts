import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  assignTechnician, cancelInstallation, completeAssignment, deactivateInstallation, fetchInstallation, fetchInstallations,
  moveInstallation, recordDesign, removeAssignment, replaceDeviceKey, setTimeZone,
} from './installations';
import { completeMine, fetchMyInstallation, fetchMyWork, fetchUserAssignments } from './myWork';

const json = (status: number, body: unknown, headers: Record<string, string> = {}) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json', ...headers } });
afterEach(() => { vi.unstubAllGlobals(); });

const ADDRESS = { street: '61 Pecan St', city: 'Round Rock', state: 'TX', zipCode: '78664' };
const person = (id: number, name: string) => ({ id, name });

// An installation as the API returns it (GET /api/installations/{id})
const PECAN = {
  id: 35, address: ADDRESS, customer: { id: 6, name: 'Grace Kim', status: 'Prospect' }, leadId: 128, status: 'Inspection',
  createdAt: '2026-10-02T09:15:00Z', activatedAt: null,
  timeZone: { id: 'America/Chicago', name: 'Central', fixed: false }, timeZones: [{ id: 'America/Chicago', name: 'Central' }],
  design: { systemSizeKw: 8.4, panelCount: 21, inverterType: 'Enphase IQ8M', fixed: false },
  next: { status: 'Active', allowed: true, reason: null }, canCancel: true, canDeactivate: false, deviceKey: null,
  assignments: [{
    id: 8, technician: person(8, 'Lena Park'), role: 'Inspector', assignedAt: '2026-11-19T08:05:00Z', assignedBy: person(1, 'Rosa Castillo'),
    completedAt: '2026-11-21T10:40:00Z', completedBy: person(8, 'Lena Park'), canComplete: false, canRemove: false,
  }],
  history: [{ at: '2026-11-19T08:00:00Z', kind: 'Status', from: 'Installation', to: 'Inspection', by: person(1, 'Rosa Castillo'), note: null, technician: null, role: null }],
};

const sent = (fetchMock: ReturnType<typeof vi.fn<typeof fetch>>) => fetchMock.mock.calls.map(([url, init]) => {
  const headers = new Headers(init?.headers);
  return `${init?.method ?? 'GET'} ${String(url)}${init?.body ? ` ${init.body}` : ''}${headers.get('If-Match') ? ` If-Match ${headers.get('If-Match')}` : ''}`;
});

describe('the installations', () => {
  it("asks for each of the filter's statuses, and a page of 20", async () => {
    const fetchMock = vi.fn<typeof fetch>(async () => json(200, { items: [], page: 1, pageSize: 20, totalCount: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await fetchInstallations(['Cancelled', 'Deactivated'], 2);
    expect(sent(fetchMock)).toEqual(['GET /api/installations?status=Cancelled&status=Deactivated&page=2&pageSize=20']);
  });

  it('returns one checked, with its version from the ETag, or none when the API sends none', async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValueOnce(json(200, PECAN, { ETag: '"4711"' })).mockResolvedValueOnce(json(200, PECAN)));
    await expect(fetchInstallation(35)).resolves.toEqual({ data: PECAN, etag: '"4711"' });
    await expect(fetchInstallation(35)).resolves.toEqual({ data: PECAN, etag: null });
  });

  it('sends each change to its address with the version it was made on, and keeps the new one', async () => {
    const fetchMock = vi.fn<typeof fetch>(async () => json(200, PECAN, { ETag: '"4712"' }));
    vi.stubGlobal('fetch', fetchMock);
    const c = { id: 35, etag: '"4711"' };
    await expect(moveInstallation(c, 'Active')).resolves.toEqual({ data: PECAN, etag: '"4712"', issuedKey: null });
    await cancelInstallation(c, 'Moving away.');
    await deactivateInstallation(c, null);
    await recordDesign(c, { systemSizeKw: 8.4, panelCount: 21, inverterType: null });
    await setTimeZone(c, 'America/Denver');
    await assignTechnician(c, 7, 'Installer');
    await removeAssignment(c, 5);
    await completeAssignment(c, 5);
    await replaceDeviceKey(c);
    expect(sent(fetchMock)).toEqual([
      'POST /api/installations/35/status {"status":"Active"} If-Match "4711"',
      'POST /api/installations/35/cancel {"reason":"Moving away."} If-Match "4711"',
      'POST /api/installations/35/deactivate {"note":null} If-Match "4711"',
      'PUT /api/installations/35/design {"systemSizeKw":8.4,"panelCount":21,"inverterType":null} If-Match "4711"',
      'PUT /api/installations/35/time-zone {"timeZone":"America/Denver"} If-Match "4711"',
      'POST /api/installations/35/assignments {"technicianId":7,"role":"Installer"} If-Match "4711"',
      'DELETE /api/installations/35/assignments/5 If-Match "4711"',
      'POST /api/installations/35/assignments/5/complete If-Match "4711"',
      'POST /api/installations/35/device-key If-Match "4711"',
    ]);
  });

  it('gives the device key from switching on apart from the installation, so it is never cached with it', async () => {
    const key = 'ost46n6gr6ny.mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDALG4';
    const active = { ...PECAN, status: 'Active', deviceKey: { prefix: 'ost46n6gr6ny', issuedAt: '2026-11-22T10:12:00Z' } };
    vi.stubGlobal('fetch', vi.fn<typeof fetch>(async () => json(200, { ...active, issuedKey: key }, { ETag: '"4712"' })));
    const changed = await moveInstallation({ id: 35, etag: '"4711"' }, 'Active');
    expect(changed).toEqual({ data: active, etag: '"4712"', issuedKey: key });
    expect(JSON.stringify(changed.data)).not.toContain(key);
  });

  it('sends no If-Match without a version', async () => {
    const fetchMock = vi.fn<typeof fetch>(async () => json(200, PECAN));
    vi.stubGlobal('fetch', fetchMock);
    await moveInstallation({ id: 35, etag: null }, 'Active');
    expect(sent(fetchMock)).toEqual(['POST /api/installations/35/status {"status":"Active"}']);
  });

  it("keeps the code of a move its condition doesn't allow yet", async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>(async () => json(409, { code: 'installation.transition_not_allowed', detail: "Can't move to Design yet. Assign a surveyor, and have them complete the survey." })));
    await expect(moveInstallation({ id: 35, etag: null }, 'Design')).rejects.toMatchObject({ kind: 'conflict', code: 'installation.transition_not_allowed' });
  });
});

describe('My work', () => {
  const item = { installationId: 36, address: ADDRESS, customerName: 'Owen Hart', status: 'Installation', role: 'Installer', assignmentId: 5, completedAt: null };
  const mine = {
    installationId: 36, address: ADDRESS, customer: { name: 'Owen Hart', phone: '(303) 555-0161' }, status: 'Installation',
    myAssignments: [{ id: 5, role: 'Installer', completedAt: null, canComplete: true, why: null }],
    team: [{ name: 'Marco Díaz', role: 'Installer', completedAt: '2026-11-21T08:15:00Z' }],
  };

  it("lists a technician's work, and a technician's assignments under Users", async () => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(json(200, [item]))
      .mockResolvedValueOnce(json(200, [{ installationId: 36, address: ADDRESS, customerName: 'Owen Hart', status: 'Installation', role: 'Installer', completedAt: null }]));
    vi.stubGlobal('fetch', fetchMock);
    await expect(fetchMyWork()).resolves.toEqual([item]);
    await fetchUserAssignments(7);
    expect(sent(fetchMock)).toEqual(['GET /api/my-work', 'GET /api/users/7/assignments']);
  });

  it('marks their own part complete with the version it was shown', async () => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(json(200, mine, { ETag: '"9"' }))
      .mockResolvedValueOnce(json(200, mine, { ETag: '"10"' }));
    vi.stubGlobal('fetch', fetchMock);
    const shown = await fetchMyInstallation(36);
    await expect(completeMine(36, 5, shown.etag)).resolves.toEqual({ data: mine, etag: '"10"' });
    expect(sent(fetchMock)).toEqual(['GET /api/my-work/36', 'POST /api/my-work/36/assignments/5/complete If-Match "9"']);
  });
});
