#!/usr/bin/env bash
#
# reset-demos.sh — stop every process and container the durable-execution
# demos (02a–02e) may leave running, so the next demo starts from a clean slate.
#
# Safe to run any time: it only targets this repo's demo workloads and is a
# no-op for anything that isn't currently running. Run on demand, or it is
# wired into the reset scene of .demo/12-wrap.yaml.
#
#   Cadence   (02a) — docker compose stack + Spring Boot worker
#   Temporal  (02b) — `temporal server start-dev` + Spring Boot worker
#   Conductor (02c) — docker compose stack + ASP.NET app
#   Dapr      (02d) — `aspire run` (Valkey + sidecar + Diagrid dashboard)
#   Restate   (02e) — `restate-server` + Node (tsx watch) app
#
set -uo pipefail

# Resolve the repo root from this script's location, so it works from any cwd.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

log()  { printf '\033[36m▸ %s\033[0m\n' "$*"; }
ok()   { printf '\033[32m  ✓ %s\033[0m\n' "$*"; }
skip() { printf '\033[90m  · %s\033[0m\n' "$*"; }

# Gracefully stop processes whose command line matches a pattern (TERM, then KILL).
kill_pattern() {
  local label="$1" pattern="$2"
  local pids
  pids="$(pgrep -if "$pattern" 2>/dev/null || true)"
  if [[ -z "$pids" ]]; then
    skip "$label — not running"
    return
  fi
  log "Stopping $label"
  pkill -if -TERM "$pattern" 2>/dev/null || true
  sleep 2
  if pgrep -if "$pattern" >/dev/null 2>&1; then
    pkill -if -KILL "$pattern" 2>/dev/null || true
  fi
  ok "$label stopped"
}

# Free a TCP port by killing whatever is listening on it.
free_port() {
  local port="$1" label="$2"
  local pids
  pids="$(lsof -ti "tcp:${port}" -sTCP:LISTEN 2>/dev/null || true)"
  if [[ -z "$pids" ]]; then
    skip "port $port ($label) — free"
    return
  fi
  log "Freeing port $port ($label)"
  kill -TERM $pids 2>/dev/null || true
  sleep 1
  pids="$(lsof -ti "tcp:${port}" -sTCP:LISTEN 2>/dev/null || true)"
  [[ -n "$pids" ]] && kill -KILL $pids 2>/dev/null || true
  ok "port $port freed"
}

# Tear down a docker compose stack (containers + volumes + orphans).
compose_down() {
  local label="$1" file="$2"
  if ! command -v docker >/dev/null 2>&1; then return; fi
  if [[ ! -f "$file" ]]; then skip "$label compose file missing — $file"; return; fi
  log "Tearing down $label docker compose stack"
  docker compose -f "$file" down -v --remove-orphans >/dev/null 2>&1 || true
  ok "$label stack down"
}

# Force-remove a container by name if it still exists (compose fallback / Aspire).
remove_container() {
  local name="$1"
  if ! command -v docker >/dev/null 2>&1; then return; fi
  if docker ps -aq -f "name=^${name}$" 2>/dev/null | grep -q .; then
    docker rm -f "$name" >/dev/null 2>&1 || true
    ok "removed container $name"
  fi
}

echo
log "Resetting durable-execution demos — repo: $REPO_ROOT"
echo

# --- 1. Application workers --------------------------------------------------
# The Cadence/Temporal Java workers and the Conductor/Dapr .NET apps all carry
# "enterprisediagnostics" in their command line, so one pattern covers them.
kill_pattern "diagnostics app workers (Java + .NET)" "enterprisediagnostics"
kill_pattern "Maven Spring Boot runner"             "spring-boot:run"
kill_pattern "Restate Node app (tsx watch)"         "tsx watch"

# --- 2. Engines / servers ----------------------------------------------------
kill_pattern "Temporal dev server" "temporal server start-dev"
kill_pattern "restate-server"      "restate-server"

# --- 3. Dapr / Aspire stack --------------------------------------------------
kill_pattern "Aspire host"   "aspire run"
kill_pattern "Dapr sidecar"  "daprd"

# --- 4. Docker compose stacks ------------------------------------------------
compose_down "Cadence"   "$REPO_ROOT/demos/01-cadence/docker-compose.yml"
compose_down "Conductor" "$REPO_ROOT/demos/03-conductor/docker-compose.yml"

# --- 5. Named containers (compose fallback + Aspire-managed) -----------------
log "Removing any leftover demo containers"
for c in cadence-cassandra cadence-server cadence-web \
         conductor-standalone workflow-state diagrid-dashboard; do
  remove_container "$c"
done

# --- 6. Free well-known demo ports (catch-all for stragglers) ----------------
free_port 8080  "worker / restate ingress / conductor API"
free_port 7233  "temporal gRPC"
free_port 8233  "temporal web"
free_port 5050  "conductor app"
free_port 5002  "conductor UI"
free_port 3000  "restate express wrapper"
free_port 9070  "restate admin"
free_port 9080  "restate handler endpoint"
free_port 58888 "diagrid dev dashboard"

echo
ok "Demo reset complete."
echo
