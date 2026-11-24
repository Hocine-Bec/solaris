// P4a (docs/measurements.md §9): the backfill storm. Every installation uploads DAYS days of readings in batches of
// 96, as 50 devices at once, and a fifth of the batches are sent a second time, some while the first is still in
// flight. The run's script counts the duplicates in the database afterwards; here, the answers and the rates.
import http from 'k6/http';
import { Counter, Rate, Trend } from 'k6/metrics';
import { SharedArray } from 'k6/data';
import exec from 'k6/execution';

const DEVICES = Number(__ENV.DEVICES);
const DAYS = Number(__ENV.DAYS);
const SLOTS = 96;
const RETRY_SHARE = Number(__ENV.RETRY_SHARE);
// Well under what the smallest system (4 kW) may produce in a slot: 4 × 0.25 × 1.1 = 1.1 kWh (BR-T1)
const WH_PER_SLOT = 400;

const accepted = new Counter('p4a_accepted');
const duplicates = new Counter('p4a_duplicates');
const rejected = new Counter('p4a_rejected');
const failures = new Counter('p4a_errors');
const batchMs = new Trend('p4a_batch_ms', true);
const ok = new Rate('p4a_ok');

// The keys the generator wrote: one line per installation, "installation_id,key"
const keys = new SharedArray('device keys', () => {
  const lines = open(__ENV.KEYS_PATH).split('\n').slice(1);
  return lines.filter(line => line.trim() !== '').map(line => line.split(',')[1].trim());
});

// Every batch to send: each installation's days, then the fifth that are sent again. Shuffled with a fixed order so
// a retry usually overlaps its original rather than following it immediately.
const batches = new SharedArray('batches', () => {
  const all = [];
  for (let device = 0; device < keys.length; device++)
    for (let day = 1; day <= DAYS; day++) all.push({ device, day, retry: false });
  const retries = [];
  for (let i = 0; i < all.length; i++)
    if (i % Math.round(1 / RETRY_SHARE) === 0) retries.push({ ...all[i], retry: true });
  const together = all.concat(retries);
  // A fixed shuffle (a linear congruential walk), so every run sends them in the same order
  let seed = 42;
  for (let i = together.length - 1; i > 0; i--) {
    seed = (seed * 1103515245 + 12345) % 2147483648;
    const j = seed % (i + 1);
    [together[i], together[j]] = [together[j], together[i]];
  }
  return together;
});

export const options = {
  scenarios: {
    storm: {
      executor: 'shared-iterations',
      vus: DEVICES,
      iterations: batches.length,
      maxDuration: '2h',
    },
  },
  summaryTrendStats: ['p(50)', 'p(95)', 'p(99)', 'max', 'count'],
};

http.setResponseCallback(http.expectedStatuses({ min: 100, max: 599 }));

/** The slots of one UTC day, DAYS back from yesterday: always on the quarter hour, always in the past (BR-T1). */
function readingsFor(day) {
  const midnight = new Date();
  midnight.setUTCHours(0, 0, 0, 0);
  midnight.setUTCDate(midnight.getUTCDate() - day);
  const readings = [];
  for (let slot = 0; slot < SLOTS; slot++)
    readings.push({
      slotStart: new Date(midnight.getTime() + slot * 15 * 60 * 1000).toISOString(),
      energyWh: WH_PER_SLOT,
    });
  return readings;
}

export default function () {
  const batch = batches[exec.scenario.iterationInTest];
  const response = http.post(`${__ENV.BASE_URL}/api/telemetry/readings`,
    JSON.stringify({ readings: readingsFor(batch.day) }),
    { headers: { 'Content-Type': 'application/json', 'X-Device-Key': keys[batch.device] } });

  batchMs.add(response.timings.duration);
  ok.add(response.status === 200);
  if (response.status !== 200) {
    failures.add(1);
    return;
  }
  const answer = response.json();
  accepted.add(answer.accepted);
  duplicates.add(answer.duplicates);
  rejected.add(answer.rejected.length);
}
