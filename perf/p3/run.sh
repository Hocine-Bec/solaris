#!/usr/bin/env bash
# P3: duplicates and lost updates under concurrency (docs/measurements.md §8).
#
#   perf/p3/run.sh <a|b> <run number> [before|after]
#
#   a  20 rounds of 50 identical quote requests at once (each round a fresh email and address): how many leads and
#      notifications each round makes, and the answers by status
#   b  200 installations at Design; for each, a move to Permits and a cancellation at the same moment: lost updates,
#      invalid histories, inconsistent statuses, and the 409s
#
# The design is whatever is checked out: the before at the tag p3-before, the after once the fixes are merged. The
# third argument names the results and, for b, says whether the requests send If-Match (after) or not (before).
# Output: perf/results/p3/<date>-<before|after>/<a|b>-run<N>/ (or RESULTS_DIR).
set -euo pipefail
source "$(dirname "$0")/../lib/common.sh"

PART=${1:?usage: perf/p3/run.sh <a|b> <run number> [before|after]}
RUN=${2:?usage: perf/p3/run.sh <a|b> <run number> [before|after]}
DESIGN=${3:-before}
case "$DESIGN" in before|after) ;; *) echo "The design must be before or after." >&2; exit 2 ;; esac
ROUNDS=${ROUNDS:-20}
COPIES=${COPIES:-50}
INSTALLATIONS=${INSTALLATIONS:-200}

OUT="${RESULTS_DIR:-$REPO/perf/results/p3/$(date +%F)-$DESIGN}/$PART-run$RUN"
mkdir -p "$OUT"
trap 'stop_api; stop_database' EXIT

# run_k6 <output dir> <k6 arguments…>: runs k6 in the background, and samples the CPU clock (into $mhz) SAMPLE_AFTER
# seconds after the script says its measured load starts (a line in its output), then waits for it to end.
SAMPLE_AFTER=${SAMPLE_AFTER:-0.3}
run_k6() {
  local out=$1; shift
  k6 run --quiet --no-color "$@" >"$out/k6-output.txt" 2>&1 &
  local pid=$!
  until grep -q 'P3 load starts' "$out/k6-output.txt" 2>/dev/null || ! kill -0 "$pid" 2>/dev/null; do sleep 0.05; done
  sleep "$SAMPLE_AFTER"
  mhz=$(cpu_mhz)
  wait "$pid"
}

build_api
start_database
start_api "$OUT/api.log" Outbox__MaxConcurrentSends=20
warm_up

case "$PART" in
  a)
    log "P3a, run $RUN ($DESIGN): $ROUNDS rounds of $COPIES identical requests"
    run_k6 "$OUT" -e BASE_URL="$BASE_URL" -e ROUNDS="$ROUNDS" -e COPIES="$COPIES" -e RUN_ID="a$RUN" \
      --summary-export "$OUT/k6-summary.json" "$REPO/perf/p3/leads.js"
    sql "with per_round as (
           select l.email, count(distinct l.id) as leads, count(m.id) as notifications
           from leads l left join outbox_messages m on m.lead_id = l.id
           where l.email like 'p3a-a$RUN-%' group by l.email)
         select json_build_object(
           'part', 'P3a', 'run', $RUN, 'design', '$DESIGN', 'rounds', $ROUNDS, 'copies', $COPIES,
           'rounds_seen', (select count(*) from per_round),
           'rounds_with_more_than_one_lead', (select count(*) from per_round where leads > 1),
           'max_leads_in_one_round', (select coalesce(max(leads), 0) from per_round),
           'leads', (select coalesce(sum(leads), 0) from per_round),
           'notifications', (select coalesce(sum(notifications), 0) from per_round),
           'extra_notifications', (select coalesce(sum(notifications), 0) - 2 * count(*) from per_round))" |
      jq --slurpfile k6 "$OUT/k6-summary.json" '. + {
        responses_201: ($k6[0].metrics.p3a_201.count // 0),
        responses_200: ($k6[0].metrics.p3a_200.count // 0),
        responses_5xx: ($k6[0].metrics.p3a_5xx.count // 0),
        responses_other: ($k6[0].metrics.p3a_other.count // 0)
      }' >"$OUT/results.json"
    write_env "$OUT/env.md" "perf/p3/leads.js ($ROUNDS rounds of $COPIES identical requests, one http.batch each)" "$mhz" \
      "Rate limiter off. Each round's requests share one email and one address."
    log "done: $(jq -c '{rounds_with_more_than_one_lead, max_leads_in_one_round, extra_notifications, responses_201, responses_200, responses_5xx}' "$OUT/results.json")"
    ;;
  b)
    log "P3b, run $RUN ($DESIGN): $INSTALLATIONS installations, a move and a cancellation at once"
    run_k6 "$OUT" -e BASE_URL="$BASE_URL" -e INSTALLATIONS="$INSTALLATIONS" -e MODE="$DESIGN" -e RUN_ID="b$RUN" \
      -e ADMIN_EMAIL="$PERF_ADMIN_EMAIL" -e ADMIN_PASSWORD="$PERF_ADMIN_PASSWORD" \
      --summary-export "$OUT/k6-summary.json" "$REPO/perf/p3/installations.js"
    sql "with measured as (
           select i.id, i.status from installations i join leads l on l.id = i.lead_id where l.email like 'p3b-b$RUN-%'),
         last_row as (
           select distinct on (h.installation_id) h.installation_id, h.to_status
           from installation_status_history h join measured m on m.id = h.installation_id
           order by h.installation_id, h.changed_at desc, h.id desc)
         select json_build_object(
           'part', 'P3b', 'run', $RUN, 'design', '$DESIGN',
           'installations', (select count(*) from measured),
           'invalid_histories', (select count(*) from (
              select h.installation_id from installation_status_history h join measured m on m.id = h.installation_id
              where h.from_status = 'Design' group by h.installation_id having count(*) > 1) x),
           'inconsistent', (select count(*) from measured m join last_row r on r.installation_id = m.id where r.to_status <> m.status),
           'final_permits', (select count(*) from measured where status = 'Permits'),
           'final_cancelled', (select count(*) from measured where status = 'Cancelled'))" |
      jq --slurpfile k6 "$OUT/k6-summary.json" '. + {
        lost_updates: ($k6[0].metrics.p3b_both_2xx.count // 0),
        one_success: ($k6[0].metrics.p3b_one_2xx.count // 0),
        no_success: ($k6[0].metrics.p3b_no_2xx.count // 0),
        responses_409: ($k6[0].metrics.p3b_409.count // 0),
        responses_5xx: ($k6[0].metrics.p3b_5xx.count // 0),
        responses_other: ($k6[0].metrics.p3b_other.count // 0)
      }' >"$OUT/results.json"
    write_env "$OUT/env.md" "perf/p3/installations.js ($INSTALLATIONS installations, each move and cancellation in one http.batch; If-Match: $([ "$DESIGN" = after ] && echo sent || echo not sent))" "$mhz" \
      "Rate limiter off. The installations are made in setup through the API: lead, contacted, converted, a surveyor's part completed, Design, design data."
    log "done: $(jq -c '{lost_updates, invalid_histories, inconsistent, responses_409, responses_5xx}' "$OUT/results.json")"
    ;;
  *) echo "The part must be a or b." >&2; exit 2 ;;
esac
