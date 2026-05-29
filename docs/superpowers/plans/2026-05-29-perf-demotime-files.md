# Per-engine Performance-Test DemoTime Files Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create five DemoTime YAML files (`.demo/90`–`94`) that each drive the `demos/perf` benchmark end-to-end for one durable-execution engine.

**Architecture:** Each file is a self-contained `version: 3` DemoTime script with the same scene skeleton — start stack → start worker → run bench (`--n 100`) → show results → teardown — adapted per engine. They mirror the startup/teardown wording already proven in the matching `.demo/0X-<engine>.yaml` files. No code or driver changes.

**Tech Stack:** DemoTime YAML (VS Code extension), the existing `demos/perf` TypeScript driver (`npm run bench`), Docker Compose / Aspire / dev-server stacks per engine.

**Spec:** `docs/superpowers/specs/2026-05-29-perf-demotime-files-design.md`

**Git note:** The repo owner handles git manually. Do **not** run `git add`/`git commit` — replace the usual commit step with the YAML-validation step shown in each task.

**Shared prerequisites (state these as speaker notes, already encoded in each file's `description`):**
- The perf driver must have its deps installed once: `cd demos/perf && npm install`.
- The driver sends `testing=true`, which skips the 60s engineering-review wait.
- The driver writes results to `slides/04-performance/04-results.md`, replacing the run engine's row.

---

### Task 1: Cadence perf file

**Files:**
- Create: `.demo/90-perf-cadence.yaml`

- [ ] **Step 1: Write the file**

Create `.demo/90-perf-cadence.yaml` with exactly this content:

```yaml
title: 90 — Performance — Cadence burst (testing=true)
description: |
  Runs the demos/perf benchmark against Cadence: 100 DiagnosticsWorkflows in a
  burst with testing=true (the 60s engineering-review wait is skipped). Starts
  the Cassandra-backed Cadence cluster and the Spring Boot worker, runs the
  bench, shows the updated results table, then tears the stack down.

  Prereq: deps installed once with `cd demos/perf && npm install`.
  The driver replaces the cadence row in slides/04-performance/04-results.md.
version: 3
scenes:
  - id: cadence-perf-start-stack
    title: Start the Cadence cluster (Cassandra + server + web)
    moves:
      - action: executeTerminalCommand
        terminalId: cadence-cluster
        command: cd demos/01-cadence && docker compose up
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for Cassandra healthy + Cadence server ready, then continue.
  - id: cadence-perf-start-worker
    title: Start the Spring Boot worker (port 8080)
    moves:
      - action: executeTerminalCommand
        terminalId: cadence-app
        command: cd demos/01-cadence && ./mvnw spring-boot:run
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for 'Started EnterpriseDiagnosticsApplication', then continue.
  - id: cadence-perf-run-bench
    title: Run the benchmark — 100 workflows against Cadence
    moves:
      - action: executeTerminalCommand
        terminalId: cadence-perf
        command: cd demos/perf && npm run bench -- --engine cadence --n 100
        insertTypingMode: character-by-character
        insertTypingSpeed: 90
        autoExecute: true
      - action: waitForInput
        message: 'Wait for the [perf] summary line (wallclock + throughput), then continue.'
  - id: cadence-perf-show-results
    title: Show the updated results table
    moves:
      - action: open
        path: slides/04-performance/04-results.md
      - action: waitForInput
        message: 'Cadence row in 04-results.md is updated. Discuss the numbers, then continue to teardown.'
  - id: cadence-perf-teardown
    title: Stop the worker and tear down the Cadence cluster
    moves:
      - action: focusTerminal
        terminalId: cadence-app
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: focusTerminal
        terminalId: cadence-cluster
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: waitForTimeout
        timeout: 1000
      - action: executeTerminalCommand
        terminalId: cadence-cluster
        command: docker compose down -v
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for all Cadence containers to stop, then continue.
      - action: closeTerminal
        terminalId: cadence-perf
      - action: closeTerminal
        terminalId: cadence-app
      - action: closeTerminal
        terminalId: cadence-cluster
```

- [ ] **Step 2: Validate the YAML parses and scene ids are unique**

Run:
```bash
python3 -c "import yaml; d=yaml.safe_load(open('.demo/90-perf-cadence.yaml')); ids=[s['id'] for s in d['scenes']]; assert len(ids)==len(set(ids)), 'duplicate scene ids'; print('OK', len(ids), 'scenes')"
```
Expected: `OK 5 scenes`

---

### Task 2: Temporal perf file

**Files:**
- Create: `.demo/91-perf-temporal.yaml`

- [ ] **Step 1: Write the file**

Create `.demo/91-perf-temporal.yaml` with exactly this content:

```yaml
title: 91 — Performance — Temporal burst (testing=true)
description: |
  Runs the demos/perf benchmark against Temporal: 100 DiagnosticsWorkflows in a
  burst with testing=true (the 60s engineering-review wait is skipped). Starts
  the Temporal dev server and the Spring Boot worker, runs the bench, shows the
  updated results table, then tears the stack down.

  Prereq: deps installed once with `cd demos/perf && npm install`.
  The driver replaces the temporal row in slides/04-performance/04-results.md.
version: 3
scenes:
  - id: temporal-perf-start-stack
    title: Start the Temporal dev server
    moves:
      - action: executeTerminalCommand
        terminalId: temporal-server
        command: cd demos/02-temporal && temporal server start-dev
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for 'Server started' + Web UI URL, then continue.
  - id: temporal-perf-start-worker
    title: Start the Spring Boot worker (port 8080)
    moves:
      - action: executeTerminalCommand
        terminalId: temporal-app
        command: cd demos/02-temporal && ./mvnw spring-boot:run
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for 'Started EnterpriseDiagnosticsApplication', then continue.
  - id: temporal-perf-run-bench
    title: Run the benchmark — 100 workflows against Temporal
    moves:
      - action: executeTerminalCommand
        terminalId: temporal-perf
        command: cd demos/perf && npm run bench -- --engine temporal --n 100
        insertTypingMode: character-by-character
        insertTypingSpeed: 90
        autoExecute: true
      - action: waitForInput
        message: 'Wait for the [perf] summary line (wallclock + throughput), then continue.'
  - id: temporal-perf-show-results
    title: Show the updated results table
    moves:
      - action: open
        path: slides/04-performance/04-results.md
      - action: waitForInput
        message: 'Temporal row in 04-results.md is updated. Discuss the numbers, then continue to teardown.'
  - id: temporal-perf-teardown
    title: Stop the worker and the Temporal dev server
    moves:
      - action: focusTerminal
        terminalId: temporal-app
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: focusTerminal
        terminalId: temporal-server
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: waitForTimeout
        timeout: 1000
      - action: closeTerminal
        terminalId: temporal-perf
      - action: closeTerminal
        terminalId: temporal-app
      - action: closeTerminal
        terminalId: temporal-server
```

- [ ] **Step 2: Validate the YAML parses and scene ids are unique**

Run:
```bash
python3 -c "import yaml; d=yaml.safe_load(open('.demo/91-perf-temporal.yaml')); ids=[s['id'] for s in d['scenes']]; assert len(ids)==len(set(ids)), 'duplicate scene ids'; print('OK', len(ids), 'scenes')"
```
Expected: `OK 5 scenes`

---

### Task 3: Conductor perf file

**Files:**
- Create: `.demo/92-perf-conductor.yaml`

- [ ] **Step 1: Write the file**

Create `.demo/92-perf-conductor.yaml` with exactly this content:

```yaml
title: 92 — Performance — Conductor burst (testing=true)
description: |
  Runs the demos/perf benchmark against Conductor: 100 DiagnosticsWorkflows in a
  burst with testing=true (the 60s engineering-review wait is skipped). Starts
  the Conductor server (Docker) and the .NET worker, runs the bench, shows the
  updated results table, then tears the stack down.

  Prereq: deps installed once with `cd demos/perf && npm install`.
  Note: Conductor's local single-node burst is slow and may report errors —
  expected, not a bug. The driver replaces the conductor row in
  slides/04-performance/04-results.md.
version: 3
scenes:
  - id: conductor-perf-start-stack
    title: Start the Conductor server (Docker)
    moves:
      - action: executeTerminalCommand
        terminalId: conductor-server
        command: cd demos/03-conductor && docker compose up
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait ~60s until the container reports 'healthy', then continue.
  - id: conductor-perf-start-worker
    title: Start the .NET worker (port 5050)
    moves:
      - action: executeTerminalCommand
        terminalId: conductor-app
        command: cd demos/03-conductor && dotnet run
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for 'Now listening on http://localhost:5050' + 'Registered diagnostics_workflow', then continue.
  - id: conductor-perf-run-bench
    title: Run the benchmark — 100 workflows against Conductor
    moves:
      - action: executeTerminalCommand
        terminalId: conductor-perf
        command: cd demos/perf && npm run bench -- --engine conductor --n 100
        insertTypingMode: character-by-character
        insertTypingSpeed: 90
        autoExecute: true
      - action: waitForInput
        message: 'Wait for the [perf] summary line (wallclock + throughput), then continue.'
  - id: conductor-perf-show-results
    title: Show the updated results table
    moves:
      - action: open
        path: slides/04-performance/04-results.md
      - action: waitForInput
        message: 'Conductor row in 04-results.md is updated. Discuss the numbers, then continue to teardown.'
  - id: conductor-perf-teardown
    title: Stop the worker and tear down the Conductor server
    moves:
      - action: focusTerminal
        terminalId: conductor-app
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: focusTerminal
        terminalId: conductor-server
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: waitForTimeout
        timeout: 1000
      - action: executeTerminalCommand
        terminalId: conductor-server
        command: docker compose down -v
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for the container to stop, then continue.
      - action: closeTerminal
        terminalId: conductor-perf
      - action: closeTerminal
        terminalId: conductor-app
      - action: closeTerminal
        terminalId: conductor-server
```

- [ ] **Step 2: Validate the YAML parses and scene ids are unique**

Run:
```bash
python3 -c "import yaml; d=yaml.safe_load(open('.demo/92-perf-conductor.yaml')); ids=[s['id'] for s in d['scenes']]; assert len(ids)==len(set(ids)), 'duplicate scene ids'; print('OK', len(ids), 'scenes')"
```
Expected: `OK 5 scenes`

---

### Task 4: Dapr perf file

**Files:**
- Create: `.demo/93-perf-dapr.yaml`

Note: Dapr has **no separate worker scene** — `aspire run` boots the worker as part of the stack (matches `.demo/05-dapr.yaml`). This file therefore has 4 scenes.

- [ ] **Step 1: Write the file**

Create `.demo/93-perf-dapr.yaml` with exactly this content:

```yaml
title: 93 — Performance — Dapr burst (testing=true)
description: |
  Runs the demos/perf benchmark against Dapr: 100 DiagnosticsWorkflows in a
  burst with testing=true (the 60s engineering-review wait is skipped). One
  `aspire run` boots Valkey, the ApiService + Dapr sidecar, and the Diagrid
  dashboard (the worker is part of the stack — no separate worker scene). Runs
  the bench, shows the updated results table, then tears the stack down.

  Prereq: deps installed once with `cd demos/perf && npm install`.
  The driver targets http://localhost:5467 by default; if Aspire reassigns the
  ApiService port, re-run with `--base-url http://localhost:<port>`.
  The driver replaces the dapr row in slides/04-performance/04-results.md.
version: 3
scenes:
  - id: dapr-perf-start-stack
    title: Start Aspire — boots Valkey, ApiService + Dapr sidecar, Diagrid Dashboard
    moves:
      - action: executeTerminalCommand
        terminalId: dapr-aspire
        command: cd demos/04-dapr && aspire run
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for the Aspire dashboard to open and all resources (cache, wf-app, diagrid-dashboard) to report Running.
  - id: dapr-perf-run-bench
    title: Run the benchmark — 100 workflows against Dapr
    moves:
      - action: executeTerminalCommand
        terminalId: dapr-perf
        command: cd demos/perf && npm run bench -- --engine dapr --n 100
        insertTypingMode: character-by-character
        insertTypingSpeed: 90
        autoExecute: true
      - action: waitForInput
        message: 'Wait for the [perf] summary line (wallclock + throughput), then continue.'
  - id: dapr-perf-show-results
    title: Show the updated results table
    moves:
      - action: open
        path: slides/04-performance/04-results.md
      - action: waitForInput
        message: 'Dapr row in 04-results.md is updated. Discuss the numbers, then continue to teardown.'
  - id: dapr-perf-teardown
    title: Stop Aspire (tears down sidecar, Valkey, Diagrid Dashboard)
    moves:
      - action: focusTerminal
        terminalId: dapr-aspire
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: waitForTimeout
        timeout: 2000
      - action: closeTerminal
        terminalId: dapr-perf
      - action: closeTerminal
        terminalId: dapr-aspire
```

- [ ] **Step 2: Validate the YAML parses and scene ids are unique**

Run:
```bash
python3 -c "import yaml; d=yaml.safe_load(open('.demo/93-perf-dapr.yaml')); ids=[s['id'] for s in d['scenes']]; assert len(ids)==len(set(ids)), 'duplicate scene ids'; print('OK', len(ids), 'scenes')"
```
Expected: `OK 4 scenes`

---

### Task 5: Restate perf file

**Files:**
- Create: `.demo/94-perf-restate.yaml`

- [ ] **Step 1: Write the file**

Create `.demo/94-perf-restate.yaml` with exactly this content:

```yaml
title: 94 — Performance — Restate burst (testing=true)
description: |
  Runs the demos/perf benchmark against Restate: 100 DiagnosticsWorkflows in a
  burst with testing=true (the 60s engineering-review wait is skipped). Starts
  restate-server and the Node service, runs the bench, shows the updated results
  table, then tears the stack down.

  Prereq: deps installed once with `cd demos/perf && npm install`.
  Notes: restate-server here runs from demos/05-restate, so it writes a fresh
  restate-data/ in that folder. On a multi-homed host it can hang on "Trying to
  join cluster" — if so, relaunch with a loopback RESTATE_ADVERTISED_ADDRESS.
  The driver replaces the restate row in slides/04-performance/04-results.md.
version: 3
scenes:
  - id: restate-perf-start-stack
    title: Start restate-server
    moves:
      - action: executeTerminalCommand
        terminalId: restate-server
        command: cd demos/05-restate && restate-server
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for 'Restate server started' on :8080/:9070, then continue.
  - id: restate-perf-start-worker
    title: Start the Node service
    moves:
      - action: executeTerminalCommand
        terminalId: restate-app
        command: cd demos/05-restate && npm run dev
        insertTypingMode: instant
        autoExecute: true
      - action: waitForInput
        message: Wait for 'Deployment registered at http://localhost:9080', then continue.
  - id: restate-perf-run-bench
    title: Run the benchmark — 100 workflows against Restate
    moves:
      - action: executeTerminalCommand
        terminalId: restate-perf
        command: cd demos/perf && npm run bench -- --engine restate --n 100
        insertTypingMode: character-by-character
        insertTypingSpeed: 90
        autoExecute: true
      - action: waitForInput
        message: 'Wait for the [perf] summary line (wallclock + throughput), then continue.'
  - id: restate-perf-show-results
    title: Show the updated results table
    moves:
      - action: open
        path: slides/04-performance/04-results.md
      - action: waitForInput
        message: 'Restate row in 04-results.md is updated. Discuss the numbers, then continue to teardown.'
  - id: restate-perf-teardown
    title: Stop the Node service and restate-server
    moves:
      - action: focusTerminal
        terminalId: restate-app
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: focusTerminal
        terminalId: restate-server
      - action: sendKeybinding
        keybinding: ctrl+c
      - action: waitForTimeout
        timeout: 1000
      - action: closeTerminal
        terminalId: restate-perf
      - action: closeTerminal
        terminalId: restate-app
      - action: closeTerminal
        terminalId: restate-server
```

- [ ] **Step 2: Validate the YAML parses and scene ids are unique**

Run:
```bash
python3 -c "import yaml; d=yaml.safe_load(open('.demo/94-perf-restate.yaml')); ids=[s['id'] for s in d['scenes']]; assert len(ids)==len(set(ids)), 'duplicate scene ids'; print('OK', len(ids), 'scenes')"
```
Expected: `OK 5 scenes`

---

### Task 6: Cross-file validation

**Files:**
- Verify: all five `.demo/9*-perf-*.yaml`

- [ ] **Step 1: Confirm all five files exist, parse, and reference the results path**

Run:
```bash
python3 - <<'PY'
import glob, yaml
files = sorted(glob.glob('.demo/9*-perf-*.yaml'))
assert len(files) == 5, files
RESULTS = 'slides/04-performance/04-results.md'
import os
assert os.path.exists(RESULTS), 'results file missing'
for f in files:
    d = yaml.safe_load(open(f))
    moves = [m for s in d['scenes'] for m in s['moves']]
    benches = [m for m in moves if m.get('action')=='executeTerminalCommand' and 'npm run bench' in m.get('command','')]
    assert len(benches)==1, (f, 'expected one bench command')
    opens = [m for m in moves if m.get('action')=='open' and m.get('path')==RESULTS]
    assert len(opens)==1, (f, 'expected one open of results.md')
    print(os.path.basename(f), 'OK')
print('all 5 OK')
PY
```
Expected: each filename printed `OK`, then `all 5 OK`.

- [ ] **Step 2: Confirm engine order/numbering matches the spec**

Run:
```bash
ls .demo/9*-perf-*.yaml
```
Expected (in this order):
```
.demo/90-perf-cadence.yaml
.demo/91-perf-temporal.yaml
.demo/92-perf-conductor.yaml
.demo/93-perf-dapr.yaml
.demo/94-perf-restate.yaml
```
