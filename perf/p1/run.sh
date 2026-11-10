#!/usr/bin/env bash
# P1: how much latency does sending email add to the lead request? (docs/measurements.md §6)
#
#   perf/p1/run.sh <run number> [reference|before|after|realism|realism-sequential]
#
# The request latencies of the measured phase, then, once every notification has gone, the time to delivery of its
# leads' emails (sent_at in email_delivery_log − the lead's created_at). "before" runs the API with
# Notifications__Mode=Synchronous: the request sends its two emails itself. "realism" is the before with real Gmail
# (5 s at 1 req/s, then 30 s at 1 req/s, to plus-addresses of Email__FromAddress); "realism-sequential" sends 1 lead,
# then 10 more one at a time, so no two emails are sent at once. Both read Email__FromAddress and the three
# Email__Gmail__ secrets from the repository's .env, and print none of them. Output:
# perf/results/p1/<date>-<label>/run<N>/ (or RESULTS_DIR).
set -euo pipefail
source "$(dirname "$0")/../lib/common.sh"

RUN=${1:?usage: perf/p1/run.sh <run number> [reference|before|after|realism|realism-sequential]}
LABEL=${2:-reference}
WAIT_LIMIT_SECONDS=${WAIT_LIMIT_SECONDS:-3600}
WARMUP_RATE=2 WARMUP_SECONDS=30 RATE=10 MEASURE_SECONDS=120 EMAIL_BASE=""
SEQUENTIAL=0 WARMUP_REQUESTS=1 MEASURE_REQUESTS=10 GMAIL_REFRESH_TOKEN=""
SENDER="Recording sender, 2 s per email"
case "$LABEL" in
  before)
    MODE=Synchronous
    SETTINGS=(Notifications__Mode=Synchronous)
    ;;
  realism | realism-sequential)
    # One value from .env, without its quotes. The secrets are exported just before the API starts, so they reach it
    # without being printed or put on a command line.
    env_value() { sed -n "s/^$1=//p" "$REPO/.env" | tail -1 | sed -e 's/^"\(.*\)"$/\1/' -e "s/^'\(.*\)'$/\1/"; }
    EMAIL_BASE=$(env_value Email__FromAddress)
    GMAIL_CLIENT_ID=$(env_value Email__Gmail__ClientId)
    GMAIL_CLIENT_SECRET=$(env_value Email__Gmail__ClientSecret)
    GMAIL_REFRESH_TOKEN=$(env_value Email__Gmail__RefreshToken)
    for key in EMAIL_BASE GMAIL_CLIENT_ID GMAIL_CLIENT_SECRET GMAIL_REFRESH_TOKEN; do
      case "${!key}" in
        "" | *example.com | test-*) echo "$key: set the real value in .env first." >&2; exit 2 ;;
      esac
    done
    MODE=Synchronous
    SETTINGS=(Notifications__Mode=Synchronous Email__Provider=Gmail "Email__FromAddress=$EMAIL_BASE")
    if [ "$LABEL" = realism ]; then
      WARMUP_RATE=1 WARMUP_SECONDS=5 RATE=1 MEASURE_SECONDS=30
    else
      SEQUENTIAL=1
    fi
    SENDER="Gmail sender, real emails"
    ;;
  *)
    MODE="the default"
    SETTINGS=()
    ;;
esac
if [ "$SEQUENTIAL" = 1 ]; then
  LOAD="$WARMUP_REQUESTS lead, then $MEASURE_REQUESTS leads one at a time"
  SAMPLE_AFTER=40 # 10 s into the measured phase, which starts at 30 s
else
  LOAD="$WARMUP_SECONDS s at $WARMUP_RATE req/s, then $MEASURE_SECONDS s at $RATE req/s"
  SAMPLE_AFTER=$((WARMUP_SECONDS + MEASURE_SECONDS / 2)) # halfway through the measured phase
fi

OUT="${RESULTS_DIR:-$REPO/perf/results/p1/$(date +%F)-$LABEL}/run$RUN"
mkdir -p "$OUT"
trap 'stop_api; stop_database' EXIT

build_api
start_database
if [ -n "$GMAIL_REFRESH_TOKEN" ]; then # the realism labels only
  export Email__Gmail__ClientId=$GMAIL_CLIENT_ID Email__Gmail__ClientSecret=$GMAIL_CLIENT_SECRET \
    Email__Gmail__RefreshToken=$GMAIL_REFRESH_TOKEN
fi
start_api "$OUT/api.log" "${SETTINGS[@]}"

log "P1 run $RUN ($LABEL, notifications: $MODE): $LOAD"
k6 run --quiet --no-color -e BASE_URL="$BASE_URL" -e WARMUP_RATE="$WARMUP_RATE" -e WARMUP_SECONDS="$WARMUP_SECONDS" \
  -e RATE="$RATE" -e MEASURE_SECONDS="$MEASURE_SECONDS" -e EMAIL_BASE="$EMAIL_BASE" -e SEQUENTIAL="$SEQUENTIAL" \
  -e WARMUP_REQUESTS="$WARMUP_REQUESTS" -e MEASURE_REQUESTS="$MEASURE_REQUESTS" \
  --summary-export "$OUT/k6-summary.json" "$REPO/perf/p1/leads.js" >"$OUT/k6-output.txt" 2>&1 &
k6_pid=$!
sleep "$SAMPLE_AFTER"
mhz=$(cpu_mhz)
wait "$k6_pid" || true
log "load done; waiting until every notification has gone (at most $WAIT_LIMIT_SECONDS s)"
wait_until_drained $(($(date +%s) + WAIT_LIMIT_SECONDS))

sql "with delivery as (
    select extract(epoch from (d.sent_at - l.created_at)) * 1000 as ms
    from email_delivery_log d
    join leads l on l.id = split_part(d.notification_key, ':', 2)::int
    where l.email like '%p1-measure-%')
  select json_build_object('run', $RUN, 'label', '$LABEL',
    'measured_leads', (select count(*) from leads where email like '%p1-measure-%'),
    'deliveries', (select count(*) from delivery),
    'pending_at_cutoff', (select count(*) from hangfire.job where statename in ('Enqueued', 'Scheduled', 'Awaiting', 'Processing')),
    'time_to_delivery_p50_ms', (select round(percentile_cont(0.5) within group (order by ms)) from delivery),
    'time_to_delivery_p95_ms', (select round(percentile_cont(0.95) within group (order by ms)) from delivery))" |
  jq --slurpfile k6 "$OUT/k6-summary.json" --argjson seconds "$MEASURE_SECONDS" --argjson sequential "$SEQUENTIAL" \
    '($k6[0].metrics) as $m | . + {
    requests: $m["http_reqs{scenario:measure}"].count,
    requests_per_second: (if $sequential == 1 then null
      else ($m["http_reqs{scenario:measure}"].count / $seconds) * 100 | round / 100 end),
    p50_ms: ($m["http_req_duration{scenario:measure}"]["p(50)"] * 10 | round / 10),
    p95_ms: ($m["http_req_duration{scenario:measure}"]["p(95)"] * 10 | round / 10),
    p99_ms: ($m["http_req_duration{scenario:measure}"]["p(99)"] * 10 | round / 10),
    failed_requests: ($m["http_req_failed{scenario:measure}"].passes // 0),
    dropped_iterations: ($m.dropped_iterations.count // 0)
  }' >"$OUT/results.json"

write_env "$OUT/env.md" "perf/p1/leads.js ($LOAD)" "$mhz" \
  "$SENDER; notifications: $MODE. Latencies of the measured phase only."
log "done: $(jq -c '{requests_per_second, p50_ms, p95_ms, p99_ms, failed_requests, time_to_delivery_p95_ms}' "$OUT/results.json")"
