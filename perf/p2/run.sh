#!/usr/bin/env bash
# P2: of all the notifications that should be sent, how many are lost when the mail server is down or the API crashes?
# (docs/measurements.md §7)
#
#   perf/p2/run.sh <A|B|C> <run number> [before|after]
#
#   A  mail outage: the fault is on for 10 minutes from the start; 500 leads at 5/s
#   B  API crash:   no fault; 500 leads at 20/s; kill -9 about 10 s in, restarted at once
#   C  both:        A's fault and load, with B's crash
#
# Then it waits until no notification is pending, at most 60 minutes after the fault is turned off (A, C) or the API is
# restarted (B), and counts. Output: perf/results/p2/<date>-<before|after>/<scenario>-run<N>/ (or RESULTS_DIR).
set -euo pipefail
source "$(dirname "$0")/../lib/common.sh"

SCENARIO=${1:?usage: perf/p2/run.sh <A|B|C> <run number> [before|after]}
RUN=${2:?usage: perf/p2/run.sh <A|B|C> <run number> [before|after]}
DESIGN=${3:-before}
LEADS=${LEADS:-500}
OUTAGE_SECONDS=${OUTAGE_SECONDS:-600}
CRASH_AFTER_SECONDS=${CRASH_AFTER_SECONDS:-10}
WAIT_LIMIT_SECONDS=${WAIT_LIMIT_SECONDS:-3600}
case "$SCENARIO" in
  A) RATE=5 FAULT=true CRASH=false ;;
  B) RATE=20 FAULT=false CRASH=true ;;
  C) RATE=5 FAULT=true CRASH=true ;;
  *) echo "The scenario must be A, B or C." >&2; exit 2 ;;
esac

OUT="${RESULTS_DIR:-$REPO/perf/results/p2/$(date +%F)-$DESIGN}/$SCENARIO-run$RUN"
mkdir -p "$OUT"
trap 'stop_api; stop_database' EXIT

build_api
start_database
# The fault's state at startup, so that in C the restarted API is still cut off from the mail server.
start_api "$OUT/api.log" FaultInjection__Enabled=$FAULT
warm_up

log "P2 $SCENARIO, run $RUN ($DESIGN): $LEADS leads at $RATE/s; fault: $FAULT; crash: $CRASH"
start=$(date +%s)
k6 run --quiet --no-color -e BASE_URL="$BASE_URL" -e RATE="$RATE" -e LEADS="$LEADS" -e RUN_ID="$SCENARIO$RUN" \
  --summary-export "$OUT/k6-summary.json" "$REPO/perf/p2/leads.js" >"$OUT/k6-output.txt" 2>&1 &
k6_pid=$!

restarted=""
if $CRASH; then
  sleep "$CRASH_AFTER_SECONDS"
  crash_api
  log "API killed (kill -9)"
  start_api "$OUT/api.log" FaultInjection__Enabled=$FAULT
  restarted=$(date +%s)
  log "API restarted"
fi
mhz=$(cpu_mhz)
wait "$k6_pid" || true # k6 exits non-zero when requests fail, which a crash causes
log "load done"

if $FAULT; then
  remaining=$((start + OUTAGE_SECONDS - $(date +%s)))
  [ "$remaining" -gt 0 ] && sleep "$remaining"
  set_email_fault false
  event=$(date +%s)
  event_name="fault off"
else
  event=$restarted
  event_name="restart"
fi
log "$event_name; waiting until nothing is pending (at most $WAIT_LIMIT_SECONDS s)"
wait_until_drained $((event + WAIT_LIMIT_SECONDS))
log "counting"

sql "select json_build_object(
  'scenario', '$SCENARIO', 'run', $RUN, 'design', '$DESIGN',
  'leads', (select count(*) from leads),
  'expected', (select 2 * count(*) from leads),
  'delivered_unique', (select count(distinct notification_key) from email_delivery_log),
  'deliveries', (select count(*) from email_delivery_log),
  'duplicates', (select count(*) - count(distinct notification_key) from email_delivery_log),
  'pending_at_cutoff', (select count(*) from hangfire.job where statename in ('Enqueued', 'Scheduled', 'Awaiting', 'Processing')),
  'failed_jobs', (select count(*) from hangfire.job where statename = 'Failed'),
  'drain_seconds', (select greatest(0, round(extract(epoch from max(sent_at)) - $event)) from email_delivery_log),
  'drain_from', '$event_name')" |
  jq --slurpfile k6 "$OUT/k6-summary.json" '. + {
    lost: (.expected - .delivered_unique - .pending_at_cutoff),
    requests: $k6[0].metrics.http_reqs.count,
    failed_requests: ($k6[0].metrics.http_req_failed.passes // 0),
    dropped_iterations: ($k6[0].metrics.dropped_iterations.count // 0)
  }' >"$OUT/results.json"

write_env "$OUT/env.md" "perf/p2/leads.js (scenario $SCENARIO, $LEADS leads at $RATE/s)" "$mhz" \
  "Recording sender, 2 s per email. Drain time measured from the $event_name."
log "done: $(jq -c '{expected, delivered_unique, lost, duplicates, pending_at_cutoff, drain_seconds}' "$OUT/results.json")"
