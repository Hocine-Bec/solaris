// P3a (docs/measurements.md §8): ROUNDS rounds; each sends COPIES identical quote requests at once (one http.batch, a
// connection each), with a fresh email and address per round. The run's script counts the leads and notifications in
// the database afterwards; here, the answers by status.
import http from 'k6/http';
import { Counter } from 'k6/metrics';

const rounds = Number(__ENV.ROUNDS);
const copies = Number(__ENV.COPIES);
const created = new Counter('p3a_201');
const existing = new Counter('p3a_200');
const serverErrors = new Counter('p3a_5xx');
const other = new Counter('p3a_other');

export const options = {
  // Every copy of a round on its own connection, so they leave together
  batch: copies,
  batchPerHost: copies,
  scenarios: { rounds: { executor: 'per-vu-iterations', vus: 1, iterations: 1, maxDuration: '30m' } },
  summaryTrendStats: ['p(50)', 'p(95)', 'max', 'count'],
};

// Every answer is counted below; none is a failure to k6
http.setResponseCallback(http.expectedStatuses({ min: 100, max: 599 }));

export default function () {
  for (let round = 1; round <= rounds; round++) {
    const body = JSON.stringify({
      firstName: 'Lead',
      lastName: `Round ${round}`,
      email: `p3a-${__ENV.RUN_ID}-${round}@example.test`,
      phoneNumber: '+1 512 555 0100',
      propertyStreet: `${round} Concurrency Ave`,
      propertyCity: 'Austin',
      propertyState: 'TX',
      propertyZipCode: '78701',
      propertyCountry: 'US',
      propertyType: 'House',
      isPropertyOwner: true,
      monthlyBillRange: '100-200',
      bestTimeToContact: 'Morning',
    });
    const request = ['POST', `${__ENV.BASE_URL}/api/leads`, body, { headers: { 'Content-Type': 'application/json' } }];
    for (const r of http.batch(Array.from({ length: copies }, () => request))) {
      if (r.status === 201) created.add(1);
      else if (r.status === 200) existing.add(1);
      else if (r.status >= 500) serverErrors.add(1);
      else other.add(1);
    }
  }
}
