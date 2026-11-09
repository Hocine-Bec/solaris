#!/usr/bin/env bash
# P1: how much latency does sending email add to the lead request? (docs/measurements.md §6)
#
#   perf/p1/run.sh <run number> [reference|before|after]
#
# The request latencies of the measured phase, then, once every notification has gone, the time to delivery of its
# leads' emails (sent_at in email_delivery_log − the lead's created_at). "before" runs the API with
# Notifications__Mode=Synchronous: the request sends its two emails itself. Output:
# perf/results/p1/<date>-<label>/run<N>/ (or RESULTS_DIR).
set -euo pipefail
source "$(dirname "$0")/../lib/common.sh"

RUN=${1:?usage: perf/p1/run.sh <run number> [reference|before|after]}
LABEL=${2:-reference}
WAIT_LIMIT_SECONDS=${WAIT_LIMIT_SECONDS:-3600}
if [ "$LABEL" = before ]; then
  MODE=Synchronous
  SETTINGS=(Notifications__Mode=Synchronous)
else
  MODE="the default"
  SETTINGS=()
fi

OUT="${RESULTS_DIR:-$REPO/perf/results/p1/$(date +%F)-$LABEL}/run$RUN"
mkdir -p "$OUT"
trap 'stop_api; stop_database' EXIT

build_api
start_database
start_api "$OUT/api.log" "${SETTINGS[@]}"

log "P1 run $RUN ($LABEL, notifications: $MODE): 30 s at 2 req/s, then 2 minutes at 10 req/s"
k6 run --quiet --no-color -e BASE_URL="$BASE_URL" --summary-export "$OUT/k6-summary.json" "$REPO/perf/p1/leads.js" \
  >"$OUT/k6-output.txt" 2>&1 &
k6_pid=$!
sleep 90
mhz=$(cpu_mhz)
wait "$k6_pid" || true
log "load done; waiting until every notification has gone (at most $WAIT_LIMIT_SECONDS s)"
wait_until_drained $(($(date +%s) + WAIT_LIMIT_SECONDS))

sql "with delivery as (
    select extract(epoch from (d.sent_at - l.created_at)) * 1000 as ms
    from email_delivery_log d
    join leads l on l.id = split_part(d.notification_key, ':', 2)::int
    where l.email like 'p1-measure-%')
  select json_build_object('run', $RUN, 'label', '$LABEL',
    'measured_leads', (select count(*) from leads where email like 'p1-measure-%'),
    'deliveries', (select count(*) from delivery),
    'pending_at_cutoff', (select count(*) from hangfire.job where statename in ('Enqueued', 'Scheduled', 'Awaiting', 'Processing')),
    'time_to_delivery_p50_ms', (select round(percentile_cont(0.5) within group (order by ms)) from delivery),
    'time_to_delivery_p95_ms', (select round(percentile_cont(0.95) within group (order by ms)) from delivery))" |
  jq --slurpfile k6 "$OUT/k6-summary.json" '($k6[0].metrics) as $m | . + {
    requests: $m["http_reqs{scenario:measure}"].count,
    requests_per_second: (($m["http_reqs{scenario:measure}"].count / 120) * 100 | round / 100),
    p50_ms: ($m["http_req_duration{scenario:measure}"]["p(50)"] * 10 | round / 10),
    p95_ms: ($m["http_req_duration{scenario:measure}"]["p(95)"] * 10 | round / 10),
    p99_ms: ($m["http_req_duration{scenario:measure}"]["p(99)"] * 10 | round / 10),
    failed_requests: ($m["http_req_failed{scenario:measure}"].passes // 0),
    dropped_iterations: ($m.dropped_iterations.count // 0)
  }' >"$OUT/results.json"

write_env "$OUT/env.md" "perf/p1/leads.js (30 s at 2 req/s, then 120 s at 10 req/s)" "$mhz" \
  "Recording sender, 2 s per email; notifications: $MODE. Latencies of the 10 req/s phase only."
log "done: $(jq -c '{requests_per_second, p50_ms, p95_ms, p99_ms, failed_requests, time_to_delivery_p95_ms}' "$OUT/results.json")"
