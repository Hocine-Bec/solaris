// P3b (docs/measurements.md §8): INSTALLATIONS installations at Design, their design recorded (made in setup through
// the API, as the office makes them). Then, for each, two changes at the same moment (one http.batch): A moves it to
// Permits, B cancels it with a reason. With MODE=after, both send the ETag read first (If-Match); with MODE=before,
// neither does. The run's script counts the histories in the database afterwards; here, the answers. Setup sends
// each installation's last ETag with its next change, when the API sends one.
import http from 'k6/http';
import { Counter } from 'k6/metrics';

const count = Number(__ENV.INSTALLATIONS);
const after = __ENV.MODE === 'after';
const bothSucceeded = new Counter('p3b_both_2xx');
const oneSucceeded = new Counter('p3b_one_2xx');
const neitherSucceeded = new Counter('p3b_no_2xx');
const conflicts = new Counter('p3b_409');
const serverErrors = new Counter('p3b_5xx');
const other = new Counter('p3b_other');

export const options = {
  setupTimeout: '15m',
  batch: 2,
  batchPerHost: 2,
  scenarios: { pairs: { executor: 'per-vu-iterations', vus: 1, iterations: 1, maxDuration: '30m' } },
  summaryTrendStats: ['p(50)', 'p(95)', 'max', 'count'],
};

http.setResponseCallback(http.expectedStatuses({ min: 100, max: 599 }));

const json = { 'Content-Type': 'application/json' };

// The line the run's script waits for, to sample the CPU clock during the measured load rather than the setup
const LOAD_STARTS = 'P3 load starts';

function must(response, status, what) {
  if (response.status !== status) throw new Error(`${what}: ${response.status} ${response.body}`);
  return response.body ? response.json() : null;
}

// The request's parameters with If-Match set to the version an answer gave, when it gave one (BR-I4)
const on = (params, answer) => {
  const etag = answer.headers.Etag;
  return etag ? { headers: { ...params.headers, 'If-Match': etag } } : params;
};

export function setup() {
  const base = __ENV.BASE_URL;
  const login = http.post(`${base}/api/auth/login`, JSON.stringify({ email: __ENV.ADMIN_EMAIL, password: __ENV.ADMIN_PASSWORD }), { headers: json });
  must(login, 200, 'sign in');
  const token = login.cookies.solaris_session[0].value;
  const admin = { headers: { ...json, Authorization: `Bearer ${token}` } };
  const surveyor = must(http.post(`${base}/api/users`, JSON.stringify({ name: 'Measurement Surveyor', email: 'p3b.surveyor@example.test', password: 'Survey#2026x' }), admin), 201, 'technician');

  const ids = [];
  for (let n = 1; n <= count; n++) {
    const lead = must(http.post(`${base}/api/leads`, JSON.stringify({
      firstName: 'Installation', lastName: `Number ${n}`, email: `p3b-${__ENV.RUN_ID}-${n}@example.test`, phoneNumber: '+1 512 555 0100',
      propertyStreet: `${n} Concurrency Blvd`, propertyCity: 'Austin', propertyState: 'TX', propertyZipCode: '78701', propertyCountry: 'US',
      propertyType: 'House', isPropertyOwner: true, monthlyBillRange: '100-200', bestTimeToContact: 'Morning',
    }), { headers: json }), 201, 'lead');
    must(http.put(`${base}/api/leads/${lead.id}/status`, JSON.stringify({ status: 'Contacted' }), admin), 200, 'contacted');
    const id = must(http.post(`${base}/api/leads/${lead.id}/convert`, null, admin), 200, 'convert').installationId;
    let last = http.get(`${base}/api/installations/${id}`, admin);
    must(last, 200, 'read');
    last = http.post(`${base}/api/installations/${id}/assignments`, JSON.stringify({ technicianId: surveyor.id, role: 'Surveyor' }), on(admin, last));
    const assigned = must(last, 200, 'assign');
    last = http.post(`${base}/api/installations/${id}/assignments/${assigned.assignments[0].id}/complete`, null, on(admin, last));
    must(last, 200, 'survey done');
    last = http.post(`${base}/api/installations/${id}/status`, JSON.stringify({ status: 'Design' }), on(admin, last));
    must(last, 200, 'design');
    must(http.put(`${base}/api/installations/${id}/design`, JSON.stringify({ systemSizeKw: 8.4, panelCount: 21, inverterType: 'Measurement inverter' }), on(admin, last)), 200, 'design data');
    ids.push(id);
  }
  return { token, ids };
}

export default function ({ token, ids }) {
  const base = __ENV.BASE_URL;
  console.log(LOAD_STARTS);
  for (const id of ids) {
    const headers = { ...json, Authorization: `Bearer ${token}` };
    if (after) {
      const etag = http.get(`${base}/api/installations/${id}`, { headers }).headers.Etag;
      if (etag) headers['If-Match'] = etag;
    }
    const [move, cancel] = http.batch([
      ['POST', `${base}/api/installations/${id}/status`, JSON.stringify({ status: 'Permits' }), { headers }],
      ['POST', `${base}/api/installations/${id}/cancel`, JSON.stringify({ reason: 'Measurement: cancelled while it moved.' }), { headers }],
    ]);
    const ok = [move, cancel].filter(r => r.status >= 200 && r.status < 300).length;
    (ok === 2 ? bothSucceeded : ok === 1 ? oneSucceeded : neitherSucceeded).add(1);
    for (const r of [move, cancel]) {
      if (r.status === 409) conflicts.add(1);
      else if (r.status >= 500) serverErrors.add(1);
      else if (r.status >= 300) other.add(1);
    }
  }
}
