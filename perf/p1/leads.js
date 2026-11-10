// P1 (docs/measurements.md §6): a 30 s warm-up at 2 req/s, then POST /api/leads at a constant 10 req/s for 2 minutes
// (an open model, rule 5). Each lead has its own email and address (rule 9). The percentiles are the measured phase's.
// The realism check changes the rates, the durations and the recipients (WARMUP_RATE, WARMUP_SECONDS, RATE,
// MEASURE_SECONDS, EMAIL_BASE), or sends one lead at a time (SEQUENTIAL=1, WARMUP_REQUESTS, MEASURE_REQUESTS);
// without them, every run is the one above.
import http from 'k6/http';
import exec from 'k6/execution';

const warmupRate = Number(__ENV.WARMUP_RATE || 2);
const warmupSeconds = Number(__ENV.WARMUP_SECONDS || 30);
const rate = Number(__ENV.RATE || 10);
const measureSeconds = Number(__ENV.MEASURE_SECONDS || 120);
const sequential = __ENV.SEQUENTIAL === '1';
const warmupRequests = Number(__ENV.WARMUP_REQUESTS || 1);
const measureRequests = Number(__ENV.MEASURE_REQUESTS || 10);

http.setResponseCallback(http.expectedStatuses(201));

export const options = {
  scenarios: sequential ? {
    // One lead at a time: each request starts when the previous one has answered (a closed model, for the realism
    // check only, so no two emails are ever sent at once). The warm-up has 30 s before the measured phase starts.
    warmup: { executor: 'per-vu-iterations', vus: 1, iterations: warmupRequests, maxDuration: '30s' },
    measure: { executor: 'per-vu-iterations', vus: 1, iterations: measureRequests, startTime: '30s', maxDuration: '10m' },
  } : {
    warmup: {
      executor: 'constant-arrival-rate', rate: warmupRate, timeUnit: '1s', duration: `${warmupSeconds}s`,
      preAllocatedVUs: 10, maxVUs: 50,
    },
    measure: {
      executor: 'constant-arrival-rate', rate, timeUnit: '1s', duration: `${measureSeconds}s`,
      startTime: `${warmupSeconds}s`, preAllocatedVUs: 50, maxVUs: 200,
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
const requestsPerPhase = sequential
  ? { warmup: warmupRequests, measure: measureRequests }
  : { warmup: warmupRate * warmupSeconds, measure: rate * measureSeconds };

// Made-up addresses, or plus-addresses of a real mailbox (EMAIL_BASE), so real emails all reach that one inbox.
function emailFor(tag) {
  if (!__ENV.EMAIL_BASE) return `${tag}@example.test`;
  const [local, domain] = __ENV.EMAIL_BASE.split('@');
  return `${local}+${tag}@${domain}`;
}

export default function () {
  const phase = exec.scenario.name;
  const n = exec.scenario.iterationInTest + 1;
  if (n > requestsPerPhase[phase]) return;
  const lead = {
    firstName: 'Lead',
    lastName: `Number ${n}`,
    email: emailFor(`p1-${phase}-${n}`),
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
