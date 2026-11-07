// P1 (docs/measurements.md §6): a 30 s warm-up at 2 req/s, then POST /api/leads at a constant 10 req/s for 2 minutes
// (an open model, rule 5). Each lead has its own email and address (rule 9). The percentiles are the measured phase's.
import http from 'k6/http';
import exec from 'k6/execution';

http.setResponseCallback(http.expectedStatuses(201));

export const options = {
  scenarios: {
    warmup: { executor: 'constant-arrival-rate', rate: 2, timeUnit: '1s', duration: '30s', preAllocatedVUs: 10, maxVUs: 50 },
    measure: {
      executor: 'constant-arrival-rate', rate: 10, timeUnit: '1s', duration: '120s', startTime: '30s',
      preAllocatedVUs: 50, maxVUs: 200,
    },
  },
  summaryTrendStats: ['p(50)', 'p(95)', 'p(99)', 'count'],
  // These put the measured phase's own numbers in the summary; they are not pass/fail limits.
  thresholds: {
    'http_req_duration{scenario:measure}': ['p(99)>=0'],
    'http_req_failed{scenario:measure}': ['rate>=0'],
    'http_reqs{scenario:measure}': ['count>=0'],
  },
};

// The executor can start one iteration more than rate × duration; each phase sends exactly this many requests.
const requestsPerPhase = { warmup: 60, measure: 1200 };

export default function () {
  const phase = exec.scenario.name;
  const n = exec.scenario.iterationInTest + 1;
  if (n > requestsPerPhase[phase]) return;
  const lead = {
    firstName: 'Lead',
    lastName: `Number ${n}`,
    email: `p1-${phase}-${n}@example.test`,
    phoneNumber: '+1 512 555 0100',
    propertyStreet: `${n} ${phase === 'measure' ? 'Measurement' : 'Warmup'} Ave`,
    propertyCity: 'Austin',
    propertyState: 'TX',
    propertyZipCode: '78701',
    propertyCountry: 'US',
    propertyLatitude: 30.264,
    propertyLongitude: -97.743,
    propertyType: 'House',
    isPropertyOwner: true,
    monthlyBillRange: '$100-$200',
    bestTimeToContact: 'Morning',
  };
  http.post(`${__ENV.BASE_URL}/api/leads`, JSON.stringify(lead), { headers: { 'Content-Type': 'application/json' } });
}
