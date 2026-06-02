#!/usr/bin/env bash
#
# reset-demos.sh — stop the processes and containers the durable-execution
# demos (02a–02e) may leave running, so the next demo starts from a clean slate.
#
# Usage:
#   reset-demos.sh [demo]
#
#     reset-demos.sh            # reset ALL demos (default)
#     reset-demos.sh cadence    # reset only the Cadence   demo (02a)
#     reset-demos.sh temporal   # reset only the Temporal  demo (02b)
#     reset-demos.sh conductor  # reset only the Conductor demo (02c)
#     reset-demos.sh dapr       # reset only the Dapr      demo (02d)
#     reset-demos.sh restate    # reset only the Restate   demo (02e)
#
#   Aliases are accepted (case-insensitive): the engine name, the demo letter
#   (02a–02e), or the demo number (01–05). Since only one demo runs at a time,
#   the per-demo reset is enough between demos; the no-argument full reset is
#   the safe catch-all wired into the reset scene of .demo/12-wrap.yaml.
#
# Safe to run any time: it only targets this repo's demo workloads and is a
# no-op for anything that isn't currently running.
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

# --- Per-demo reset routines -------------------------------------------------
# Each worker pattern OR-combines the demo directory (matched via the JVM
# classpath / .NET dll path / Maven multiModuleProjectDirectory) with a marker
# unique to that engine, so a single demo can be targeted without touching its
# siblings that share the generic "enterprisediagnostics" / "spring-boot:run"
# command-line strings.

reset_cadence() {
  log "Resetting Cadence (02a)"
  kill_pattern "Cadence Java worker" "demos/01-cadence|enterprise-diagnostics-cadence|cadence-client"
  compose_down  "Cadence" "$REPO_ROOT/demos/01-cadence/docker-compose.yml"
  for c in cadence-cassandra cadence-server cadence-web; do remove_container "$c"; done
  free_port 8080 "cadence worker"
}

reset_temporal() {
  log "Resetting Temporal (02b)"
  kill_pattern "Temporal Java worker"  "demos/02-temporal|temporal-spring-boot-starter|temporal-sdk"
  kill_pattern "Temporal dev server"   "temporal server start-dev"
  free_port 8080 "temporal worker"
  free_port 7233 "temporal gRPC"
  free_port 8233 "temporal web"
}

reset_conductor() {
  log "Resetting Conductor (02c)"
  kill_pattern "Conductor .NET app" "demos/03-conductor|EnterpriseDiagnostics\.Conductor|conductor-csharp"
  compose_down  "Conductor" "$REPO_ROOT/demos/03-conductor/docker-compose.yml"
  remove_container "conductor-standalone"
  free_port 8080 "conductor API"
  free_port 5050 "conductor app"
  free_port 5002 "conductor UI"
}

reset_dapr() {
  log "Resetting Dapr (02d)"
  kill_pattern "Dapr .NET app (Aspire)" "demos/04-dapr|EnterpriseDiagnostics\.ApiService|Dapr\.Workflow"
  kill_pattern "Aspire host"            "aspire run"
  kill_pattern "Dapr sidecar"           "daprd"
  remove_container "workflow-state"
  remove_container "diagrid-dashboard"
  free_port 58888 "diagrid dev dashboard"
}

reset_restate() {
  log "Resetting Restate (02e)"
  kill_pattern "Restate Node app (tsx watch)" "demos/05-restate|tsx watch"
  kill_pattern "restate-server"               "restate-server"
  free_port 8080 "restate ingress"
  free_port 3000 "restate express wrapper"
  free_port 9070 "restate admin"
  free_port 9080 "restate handler endpoint"
}

# Full reset — the original proven sequence, kept intact as the safe default.
# It uses the broad command-line patterns (one "enterprisediagnostics" pattern
# covers every Java + .NET worker) plus a straggler sweep of every demo port.
reset_all() {
  # --- 1. Application workers ------------------------------------------------
  kill_pattern "diagnostics app workers (Java + .NET)" "enterprisediagnostics"
  kill_pattern "Maven Spring Boot runner"             "spring-boot:run"
  kill_pattern "Restate Node app (tsx watch)"         "tsx watch"

  # --- 2. Engines / servers --------------------------------------------------
  kill_pattern "Temporal dev server" "temporal server start-dev"
  kill_pattern "restate-server"      "restate-server"

  # --- 3. Dapr / Aspire stack ------------------------------------------------
  kill_pattern "Aspire host"   "aspire run"
  kill_pattern "Dapr sidecar"  "daprd"

  # --- 4. Docker compose stacks ----------------------------------------------
  compose_down "Cadence"   "$REPO_ROOT/demos/01-cadence/docker-compose.yml"
  compose_down "Conductor" "$REPO_ROOT/demos/03-conductor/docker-compose.yml"

  # --- 5. Named containers (compose fallback + Aspire-managed) ---------------
  log "Removing any leftover demo containers"
  for c in cadence-cassandra cadence-server cadence-web \
           conductor-standalone workflow-state diagrid-dashboard; do
    remove_container "$c"
  done

  # --- 6. Free well-known demo ports (catch-all for stragglers) --------------
  free_port 8080  "worker / restate ingress / conductor API"
  free_port 7233  "temporal gRPC"
  free_port 8233  "temporal web"
  free_port 5050  "conductor app"
  free_port 5002  "conductor UI"
  free_port 3000  "restate express wrapper"
  free_port 9070  "restate admin"
  free_port 9080  "restate handler endpoint"
  free_port 58888 "diagrid dev dashboard"
}

usage() {
  cat <<'EOF'
Usage: reset-demos.sh [demo]

  (no argument)            reset ALL demos
  cadence   | 02a | 01     reset only the Cadence   demo
  temporal  | 02b | 02     reset only the Temporal  demo
  conductor | 02c | 03     reset only the Conductor demo
  dapr      | 02d | 04     reset only the Dapr      demo
  restate   | 02e | 05     reset only the Restate   demo
EOF
}

# --- Dispatch ----------------------------------------------------------------
# Normalize the optional argument (case-insensitive) to an engine key.
target_raw="${1:-all}"
target="$(printf '%s' "$target_raw" | tr '[:upper:]' '[:lower:]')"

case "$target" in
  cadence|02a|01)   target="cadence"   ;;
  temporal|02b|02)  target="temporal"  ;;
  conductor|02c|03) target="conductor" ;;
  dapr|02d|04)      target="dapr"      ;;
  restate|02e|05)   target="restate"   ;;
  all)              target="all"       ;;
  -h|--help|help)   usage; exit 0      ;;
  *)
    printf '\033[31mUnknown demo: %s\033[0m\n\n' "$target_raw" >&2
    usage >&2
    exit 1
    ;;
esac

echo
if [[ "$target" == "all" ]]; then
  log "Resetting ALL durable-execution demos — repo: $REPO_ROOT"
else
  log "Resetting the $target demo — repo: $REPO_ROOT"
fi
echo

case "$target" in
  cadence)   reset_cadence   ;;
  temporal)  reset_temporal  ;;
  conductor) reset_conductor ;;
  dapr)      reset_dapr      ;;
  restate)   reset_restate   ;;
  all)       reset_all       ;;
esac

echo
ok "Demo reset complete."
echo
