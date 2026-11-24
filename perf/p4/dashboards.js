// P4b (docs/measurements.md §9): the three production views at dataset M. Each one warms up for 30 s, then takes a
// constant 10 requests a second for a minute, one endpoint after another so each one's numbers are its own. Only
// the measured minute is recorded.
import http from 'k6/http';
import { Counter, Trend } from 'k6/metrics';
import exec from 'k6/execution';

const INSTALLATIONS = Number(__ENV.INSTALLATIONS);
const RATE = Number(__ENV.RATE);
const WARMUP = __ENV.WARMUP_SECONDS;
const MEASURE = __ENV.MEASURE_SECONDS;
const YEAR = Number(__ENV.YEAR);
const MONTH = Number(__ENV.MONTH);

const views = ['fleet', 'month', 'year'];
const ms = Object.fromEntries(views.map(v => [v, new Trend(`p4b_${v}_ms`, true)]));
const errors = Object.fromEntries(views.map(v => [v, new Counter(`p4b_${v}_errors`)]));

/** Warm-up then the measured minute, one view after another: 90 seconds each. */
function scenariosFor() {
  const each = Number(WARMUP.replace('s', '')) + Number(MEASURE.replace('s', ''));
  const scenarios = {};
  views.forEach((view, n) => {
    scenarios[`${view}_warm`] = {
      executor: 'constant-arrival-rate', rate: RATE, timeUnit: '1s', duration: WARMUP,
      preAllocatedVUs: RATE * 4, maxVUs: RATE * 20, exec: view, startTime: `${n * each}s`,
    };
    scenarios[`${view}_measure`] = {
      executor: 'constant-arrival-rate', rate: RATE, timeUnit: '1s', duration: MEASURE,
      preAllocatedVUs: RATE * 4, maxVUs: RATE * 20, exec: view,
      startTime: `${n * each + Number(WARMUP.replace('s', ''))}s`,
    };
  });
  return scenarios;
}

export const options = {
  scenarios: scenariosFor(),
  summaryTrendStats: ['p(50)', 'p(95)', 'p(99)', 'max', 'count'],
};

http.setResponseCallback(http.expectedStatuses({ min: 100, max: 599 }));

export function setup() {
  const login = http.post(`${__ENV.BASE_URL}/api/auth/login`,
    JSON.stringify({ email: __ENV.ADMIN_EMAIL, password: __ENV.ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } });
  if (login.status !== 200) throw new Error(`sign in: ${login.status} ${login.body}`);
  return { token: login.cookies.solaris_session[0].value };
}

/** Records only the measured minute; the warm-up's scenario has the same name without `_measure`. */
function record(view, response) {
  if (exec.scenario.name.endsWith('_measure')) {
    ms[view].add(response.timings.duration);
    if (response.status !== 200) errors[view].add(1);
  }
}

const ask = (token, path) => http.get(`${__ENV.BASE_URL}${path}`, { headers: { Authorization: `Bearer ${token}` } });

/** A different installation each time, so no one row is read from cache all minute. */
const someInstallation = () => 1 + Math.floor(Math.random() * INSTALLATIONS);

export function fleet(data) {
  record('fleet', ask(data.token, '/api/production/fleet'));
}

export function month(data) {
  // A month the dataset covers: the one the run's script worked out, or the one before it
  const month = Math.random() < 0.5 ? MONTH : (MONTH === 1 ? 12 : MONTH - 1);
  const year = month > MONTH ? YEAR - 1 : YEAR;
  record('month', ask(data.token, `/api/production/installations/${someInstallation()}/years/${year}/months/${month}`));
}

export function year(data) {
  record('year', ask(data.token, `/api/production/installations/${someInstallation()}/years/${YEAR}`));
}
