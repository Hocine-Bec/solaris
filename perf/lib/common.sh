#!/usr/bin/env bash
# Shared by the measurement scripts (docs/measurements.md): a fresh database per run, the API's Release build in the
# Development environment, and the environment record. Sourced, not run.

REPO=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
API_DIR="$REPO/backend/src/WebAPI"
DB_CONTAINER=solaris-perf-db
DB_PORT=${DB_PORT:-55440}
API_PORT=${API_PORT:-5099}
BASE_URL="http://127.0.0.1:$API_PORT"
CONNECTION="Host=127.0.0.1;Port=$DB_PORT;Database=solaris;Username=postgres;Password=perf"
API_PID=""

log() { printf '%s %s\n' "$(date +%T)" "$*"; }

sql() { docker exec "$DB_CONTAINER" psql -U postgres -h 127.0.0.1 -d solaris -Atc "$1"; }

# A new, empty postgres:18 container: every run starts from the same state, and no other database is touched.
start_database() {
  docker rm -f "$DB_CONTAINER" >/dev/null 2>&1 || true
  docker run -d --rm --name "$DB_CONTAINER" -e POSTGRES_PASSWORD=perf -e POSTGRES_DB=solaris \
    -p "127.0.0.1:$DB_PORT:5432" postgres:18 >/dev/null
  # Over TCP, so the check waits for the final server, not the one the image runs during its first initialisation.
  for _ in $(seq 1 60); do
    sql 'select 1' >/dev/null 2>&1 && return 0
    sleep 1
  done
  echo "The database did not start." >&2
  return 1
}

stop_database() { docker rm -f "$DB_CONTAINER" >/dev/null 2>&1 || true; }

build_api() { dotnet build "$API_DIR/WebAPI.csproj" -c Release -nologo -v q >/dev/null; }

# start_api <log file> [NAME=value ...]: the Release build in Development (so the measurement harness is there and the
# migrations run), with Recording at 2 s per email, in the background; returns once it answers.
start_api() {
  local log_file=$1
  shift
  (cd "$API_DIR" && exec env ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$BASE_URL" \
    ConnectionStrings__Default="$CONNECTION" Jwt__Secret=perf-secret-perf-secret-perf-secret-perf \
    Email__FromAddress=sales@example.test Email__Provider=Recording Email__SimulatedLatencyMs=2000 \
    "$@" dotnet bin/Release/net10.0/WebAPI.dll) >>"$log_file" 2>&1 &
  API_PID=$!
  for _ in $(seq 1 240); do
    curl -sf -o /dev/null "$BASE_URL/api/leads?pageSize=1" && return 0
    kill -0 "$API_PID" 2>/dev/null || { echo "The API exited; see $log_file" >&2; return 1; }
    sleep 0.25
  done
  echo "The API did not start; see $log_file" >&2
  return 1
}

# A normal shutdown, which lets Hangfire stop its workers.
stop_api() {
  if [ -n "$API_PID" ]; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi
  API_PID=""
}

# The crash of P2 scenarios B and C: no shutdown at all.
crash_api() {
  kill -9 "$API_PID"
  wait "$API_PID" 2>/dev/null || true
  API_PID=""
}

# Reads only, so the leads a run counts are only its own.
warm_up() {
  for _ in $(seq 1 30); do
    curl -s -o /dev/null "$BASE_URL/api/leads?pageSize=5"
    curl -s -o /dev/null "$BASE_URL/api/addresses/999"
  done
}

set_email_fault() {
  curl -sf -o /dev/null -X PUT -H 'Content-Type: application/json' --data "{\"on\":$1}" "$BASE_URL/api/dev/email-fault"
}

# Hangfire jobs still to run: waiting in the queue, scheduled for a retry, or running (a job that was running when the
# API was killed stays Processing until Hangfire gives it to another worker).
pending_jobs() {
  sql "select count(*) from hangfire.job where statename in ('Enqueued', 'Scheduled', 'Awaiting', 'Processing')"
}

# wait_until_drained <deadline, epoch seconds>: until no job is pending at two checks in a row, or the deadline.
wait_until_drained() {
  local deadline=$1 quiet=0
  while [ "$(date +%s)" -lt "$deadline" ]; do
    if [ "$(pending_jobs)" = 0 ]; then
      quiet=$((quiet + 1))
      [ "$quiet" -ge 2 ] && return 0
    else
      quiet=0
    fi
    sleep 5
  done
}

cpu_mhz() {
  awk -F: '/MHz/ { printf "%d\n", $2 }' /proc/cpuinfo | sort -n | awk 'NR == 1 { low = $1 } { high = $1 } END { print low "–" high " MHz across the cores" }'
}

# write_env <file> <script> <cpu MHz sampled during the run> <notes>: the environment record (docs/measurements.md §2).
write_env() {
  local file=$1 script=$2 mhz=$3 notes=$4
  {
    echo "Date / time:    $(date -Iseconds)"
    echo "Git commit:     $(git -C "$REPO" describe --tags --always --dirty)"
    echo "Machine:        $(cat /sys/class/dmi/id/sys_vendor 2>/dev/null) $(cat /sys/class/dmi/id/product_name 2>/dev/null) · $(lscpu | sed -n 's/^Model name: *//p') ($(nproc) threads) · $(awk '/MemTotal/ { printf "%.1f GiB RAM", $2 / 1048576 }' /proc/meminfo)"
    echo "OS / kernel:    $(lsb_release -ds 2>/dev/null) · $(uname -r)"
    echo "Power profile:  $(powerprofilesctl get 2>/dev/null || echo unknown) ; CPU MHz during the run: $mhz"
    echo ".NET:           $(dotnet --version) (Release build)"
    echo "PostgreSQL:     postgres:18 in Docker · $(sql 'select version()') · configuration changes: none"
    echo "Dataset:        an empty database; the run creates its own leads"
    echo "Load tool:      $(k6 version | head -1) · $script"
    echo "Other load:     containers: $(docker ps --format '{{.Names}}' | grep -vx "$DB_CONTAINER" | paste -sd, - || true); browser: $(pgrep -x 'firefox|chrome|chromium|brave' >/dev/null && echo running || echo 'not running')"
    echo "Notes:          $notes"
  } >"$file"
}
