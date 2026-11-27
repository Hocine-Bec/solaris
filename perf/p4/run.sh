#!/usr/bin/env bash
# P4: telemetry at volume (docs/measurements.md §9).
#
#   perf/p4/run.sh <a|b> <run number> [before|after]
#
#   a  the backfill storm: 1,000 installations upload 2 days of readings each, in 2,000 batches of 96, as 50 devices
#      at once, with a fifth of the batches sent twice. Readings a second, batch p95, duplicates in the database
#   b  the three production views at dataset M (1,000 installations × 120 days ≈ 11.5 million readings): each warmed
#      up for 30 s, then a constant 10 requests a second for a minute, with EXPLAIN (ANALYZE, BUFFERS) for one call
#
# The design is whatever is checked out: the before at the tag p4-before, the after once the fixes are merged. The
# third argument names the results and picks the table layout the dataset is generated in.
# Output: perf/results/p4/<date>-<before|after>/<a|b>-run<N>/ (or RESULTS_DIR).
set -euo pipefail
source "$(dirname "$0")/../lib/common.sh"

PART=${1:?usage: perf/p4/run.sh <a|b> <run number> [before|after]}
RUN=${2:?usage: perf/p4/run.sh <a|b> <run number> [before|after]}
DESIGN=${3:-before}
case "$DESIGN" in before|after) ;; *) echo "The design must be before or after." >&2; exit 2 ;; esac
case "$PART" in a|b) ;; *) echo "The part must be a or b." >&2; exit 2 ;; esac

SCHEMA=$([ "$DESIGN" = after ] && echo final || echo naive)
INSTALLATIONS=${INSTALLATIONS:-1000}
SEED=${SEED:-42}
# P4a: the devices upload the readings themselves
STORM_DAYS=${STORM_DAYS:-2}
DEVICES=${DEVICES:-50}
RETRY_SHARE=${RETRY_SHARE:-0.2}
# P4b: the dataset the views read
DATASET_DAYS=${DATASET_DAYS:-120}
RATE=${RATE:-10}
WARMUP_SECONDS=${WARMUP_SECONDS:-30s}
MEASURE_SECONDS=${MEASURE_SECONDS:-60s}
# The CPU clock is read this long after the measured load starts, as P1's, P2's and P3's are
SAMPLE_AFTER=${SAMPLE_AFTER:-5}

OUT="${RESULTS_DIR:-$REPO/perf/results/p4/$(date +%F)-$DESIGN}/$PART-run$RUN"
KEYS="$REPO/perf/keys.csv"   # gitignored: device keys, even for a throwaway database (ADR 14)
mkdir -p "$OUT"
trap 'stop_api; stop_database' EXIT

generate() {
  local days=$1
  log "generating: $INSTALLATIONS installations, $days days, seed $SEED, $SCHEMA layout"
  dotnet run --project "$REPO/backend/tools/Solaris.DataGen" -c Release --no-build -- \
    --installations "$INSTALLATIONS" --days "$days" --seed "$SEED" --schema "$SCHEMA" \
    --connection "$CONNECTION" --keys "$KEYS" | tee "$OUT/dataset.txt"
}

# The rows the three views read, with their plans (docs/measurements.md §9). The statements are the ones the
# endpoints send, read from the API's own EF Core log and written out with their values; the fleet's view sends one
# of them per time zone its installations are in.
explain_views() {
  local year=$1 month=$2 zone installations from to
  # The zone the most installations are in: the fleet's day is one GROUP BY per zone in the before
  zone=$(sql "select time_zone from installations group by time_zone order by count(*) desc limit 1")
  installations=$(sql "select string_agg(id::text, ',') from installations where time_zone = '$zone'")
  from=$(sql "select (date_trunc('day', (now() AT TIME ZONE '$zone')) - interval '1 day') AT TIME ZONE '$zone'")
  to=$(sql "select date_trunc('day', (now() AT TIME ZONE '$zone')) AT TIME ZONE '$zone'")

  if [ "$DESIGN" = before ]; then
    sql "EXPLAIN (ANALYZE, BUFFERS)
         SELECT e.installation_id AS \"Key\", COALESCE(sum(e.energy_kwh), 0.0) AS \"Kwh\"
         FROM energy_readings AS e
         WHERE e.installation_id = ANY(ARRAY[$installations]) AND e.recorded_at >= '$from' AND e.recorded_at < '$to'
         GROUP BY e.installation_id" >"$OUT/plan-fleet.txt"
    sql "EXPLAIN (ANALYZE, BUFFERS)
         SELECT e.recorded_at AS \"RecordedAt\", e.energy_kwh AS \"EnergyKwh\"
         FROM energy_readings AS e
         WHERE e.installation_id = 1 AND e.recorded_at >= '$year-$(printf '%02d' "$month")-01'
           AND e.recorded_at < '$year-$(printf '%02d' "$month")-01'::date + interval '1 month'
         ORDER BY e.recorded_at" >"$OUT/plan-month.txt"
    sql "EXPLAIN (ANALYZE, BUFFERS)
         SELECT e.recorded_at AS \"RecordedAt\", e.energy_kwh AS \"EnergyKwh\"
         FROM energy_readings AS e
         WHERE e.installation_id = 1 AND e.recorded_at >= '$year-01-01' AND e.recorded_at < '$((year + 1))-01-01'
         ORDER BY e.recorded_at" >"$OUT/plan-year.txt"
  else
    # The fleet's day is one read of that day's rollups; the month and the year, one installation's by the key
    local day
    day=$(sql "select (now() AT TIME ZONE '$zone')::date - 1")
    sql "EXPLAIN (ANALYZE, BUFFERS)
         SELECT e.installation_id AS \"InstallationId\", e.actual_wh AS \"ActualWh\", e.reading_count AS \"ReadingCount\"
         FROM energy_production AS e
         WHERE e.day = '$day'" >"$OUT/plan-fleet.txt"
    sql "EXPLAIN (ANALYZE, BUFFERS)
         SELECT e.day AS \"Day\", e.actual_wh AS \"ActualWh\", e.reading_count AS \"ReadingCount\"
         FROM energy_production AS e
         WHERE e.installation_id = 1 AND e.day >= '$year-$(printf '%02d' "$month")-01'
           AND e.day < ('$year-$(printf '%02d' "$month")-01'::date + interval '1 month')::date
         ORDER BY e.day" >"$OUT/plan-month.txt"
    sql "EXPLAIN (ANALYZE, BUFFERS)
         SELECT e.day AS \"Day\", e.actual_wh AS \"ActualWh\", e.reading_count AS \"ReadingCount\"
         FROM energy_production AS e
         WHERE e.installation_id = 1 AND e.day >= '$year-01-01' AND e.day < '$((year + 1))-01-01'
         ORDER BY e.day" >"$OUT/plan-year.txt"
  fi
  log "plans saved: $(ls "$OUT" | grep -c '^plan-') files"
}

build_api
dotnet build "$REPO/backend/tools/Solaris.DataGen" -c Release -nologo -v q >/dev/null
start_database
start_api "$OUT/api.log"
warm_up

case "$PART" in
  a)
    # The fleet only: the devices bring the readings
    generate 0
    log "P4a, run $RUN ($DESIGN): $INSTALLATIONS devices × $STORM_DAYS days, $DEVICES at once, a fifth sent twice"
    started=$(date +%s.%N)
    k6 run --quiet --no-color -e BASE_URL="$BASE_URL" -e KEYS_PATH="$KEYS" -e DEVICES="$DEVICES" \
      -e DAYS="$STORM_DAYS" -e RETRY_SHARE="$RETRY_SHARE" \
      --summary-export "$OUT/k6-summary.json" "$REPO/perf/p4/ingest.js" >"$OUT/k6-output.txt" 2>&1 &
    k6_pid=$!
    sleep "$SAMPLE_AFTER"
    mhz=$(cpu_mhz)
    wait "$k6_pid" || true
    seconds=$(echo "$(date +%s.%N) - $started" | bc)

    rows=$(sql "select count(*) from energy_readings")
    # The slot's column is recorded_at in the before's layout and slot_start in the after's
    slot_column=$([ "$SCHEMA" = naive ] && echo recorded_at || echo slot_start)
    dupes=$(sql "select count(*) from (select 1 from energy_readings group by installation_id, $slot_column having count(*) > 1) d")
    jq -n --slurpfile k6 "$OUT/k6-summary.json" --arg seconds "$seconds" --arg rows "$rows" --arg dupes "$dupes" \
      --arg run "$RUN" --arg design "$DESIGN" --arg installations "$INSTALLATIONS" --arg days "$STORM_DAYS" '{
        part: "P4a", run: ($run | tonumber), design: $design,
        installations: ($installations | tonumber), days: ($days | tonumber),
        batches: ($k6[0].metrics.p4a_batch_ms.count // 0),
        accepted: ($k6[0].metrics.p4a_accepted.count // 0),
        duplicates_answered: ($k6[0].metrics.p4a_duplicates.count // 0),
        rejected: ($k6[0].metrics.p4a_rejected.count // 0),
        errors: ($k6[0].metrics.p4a_errors.count // 0),
        wall_seconds: ($seconds | tonumber),
        readings_per_second: (($k6[0].metrics.p4a_accepted.count // 0) / ($seconds | tonumber)),
        batch_p95_ms: $k6[0].metrics.p4a_batch_ms["p(95)"],
        batch_p99_ms: $k6[0].metrics.p4a_batch_ms["p(99)"],
        rows_in_database: ($rows | tonumber),
        duplicates_in_database: ($dupes | tonumber)
      }' >"$OUT/results.json"
    DATASET_NOTE="$INSTALLATIONS Active installations and their keys, generated with --days 0 (seed $SEED); every reading comes from the upload" \
    write_env "$OUT/env.md" "perf/p4/ingest.js ($INSTALLATIONS devices × $STORM_DAYS days in batches of 96, $DEVICES at once, a fifth sent twice)" "$mhz" \
      "Rate limiter off."
    log "done: $(jq -c '{readings_per_second, batch_p95_ms, duplicates_in_database, errors}' "$OUT/results.json")"
    ;;
  b)
    generate "$DATASET_DAYS"
    rollup='null'
    if [ "$DESIGN" = after ]; then
      # COPY bypasses the ingestion statement that marks the days, so every generated day is marked here, then all
      # of them added up at once: "how long the full rollup took" (docs/measurements.md §9)
      log "marking every generated day for the rollup"
      sql "insert into production_dirty_days (installation_id, day)
           select distinct r.installation_id, (r.slot_start at time zone i.time_zone)::date
           from energy_readings r join installations i on i.id = r.installation_id" >/dev/null
      log "rolling up: $(sql 'select count(*) from production_dirty_days') days"
      rollup=$(curl -sf -X POST "$BASE_URL/api/dev/rollups")
      echo "$rollup" >"$OUT/rollup.json"
      log "rolled up: $rollup"
    fi
    # A month the dataset covers: yesterday's
    year=$(date -u -d yesterday +%Y)
    month=$(date -u -d yesterday +%-m)
    log "P4b, run $RUN ($DESIGN): three views, $WARMUP_SECONDS warm-up then $RATE/s for $MEASURE_SECONDS each"
    k6 run --quiet --no-color -e BASE_URL="$BASE_URL" -e INSTALLATIONS="$INSTALLATIONS" -e RATE="$RATE" \
      -e WARMUP_SECONDS="$WARMUP_SECONDS" -e MEASURE_SECONDS="$MEASURE_SECONDS" -e YEAR="$year" -e MONTH="$month" \
      -e ADMIN_EMAIL="$PERF_ADMIN_EMAIL" -e ADMIN_PASSWORD="$PERF_ADMIN_PASSWORD" \
      --summary-export "$OUT/k6-summary.json" "$REPO/perf/p4/dashboards.js" >"$OUT/k6-output.txt" 2>&1 &
    k6_pid=$!
    sleep "$SAMPLE_AFTER"
    mhz=$(cpu_mhz)
    wait "$k6_pid" || true

    rows=$(sql "select count(*) from energy_readings")
    jq -n --slurpfile k6 "$OUT/k6-summary.json" --arg rows "$rows" --arg run "$RUN" --arg design "$DESIGN" \
      --argjson rollup "$rollup" '{
        part: "P4b", run: ($run | tonumber), design: $design, readings_in_database: ($rows | tonumber),
        full_rollup: $rollup,
        views: ([ "fleet", "month", "year" ] | map({
          (.): {
            requests: ($k6[0].metrics["p4b_" + . + "_ms"].count // 0),
            p50_ms: $k6[0].metrics["p4b_" + . + "_ms"]["p(50)"],
            p95_ms: $k6[0].metrics["p4b_" + . + "_ms"]["p(95)"],
            p99_ms: $k6[0].metrics["p4b_" + . + "_ms"]["p(99)"],
            errors: ($k6[0].metrics["p4b_" + . + "_errors"].count // 0)
          }}) | add)
      }' >"$OUT/results.json"
    explain_views "$year" "$month"
    DATASET_NOTE="dataset M: $INSTALLATIONS installations × $DATASET_DAYS days, seed $SEED, $SCHEMA layout; $rows readings" \
    write_env "$OUT/env.md" "perf/p4/dashboards.js (three views, $WARMUP_SECONDS warm-up then $RATE/s for $MEASURE_SECONDS each)" "$mhz" \
      "Rate limiter off. The plans are of the rows each view reads, with EXPLAIN (ANALYZE, BUFFERS), after the load."
    log "done: $(jq -c '.views | map_values({p50_ms, p95_ms, errors})' "$OUT/results.json")"
    ;;
esac
