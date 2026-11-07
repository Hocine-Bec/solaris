// P2 (docs/measurements.md §7): LEADS new leads at a constant RATE per second (an open model, rule 5). Each lead has
// its own email and address (rule 9), so only the notifications are measured.
import http from 'k6/http';
import exec from 'k6/execution';

const rate = Number(__ENV.RATE);
const leads = Number(__ENV.LEADS);

// Only 201 counts as a success; a request refused while the API is down counts as failed.
http.setResponseCallback(http.expectedStatuses(201));

export const options = {
  scenarios: {
    leads: {
      executor: 'constant-arrival-rate',
      rate,
      timeUnit: '1s',
      duration: `${leads / rate}s`,
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  summaryTrendStats: ['p(50)', 'p(95)', 'p(99)', 'count'],
};

export default function () {
  const n = exec.scenario.iterationInTest + 1;
  // The executor can start one iteration more than rate × duration; the run sends exactly LEADS requests.
  if (n > leads) return;
  const lead = {
    firstName: 'Lead',
    lastName: `Number ${n}`,
    email: `p2-${__ENV.RUN_ID}-${n}@example.test`,
    phoneNumber: '+1 512 555 0100',
    propertyStreet: `${n} Measurement Ave`,
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
