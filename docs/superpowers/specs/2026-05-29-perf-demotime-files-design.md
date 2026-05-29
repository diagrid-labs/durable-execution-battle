# Spec: Per-engine performance-test DemoTime files

**Date:** 2026-05-29
**Status:** Approved for planning

## Goal

Create one DemoTime YAML file per durable-execution engine that drives the
existing performance test (`demos/perf`) end to end for that engine. Each file
is self-contained: it starts the engine's stack, starts its worker, runs the
benchmark, shows the updated results table, and tears everything down.

## Files

Five files in `.demo/`, numbered `90`–`94`, in the order
Cadence → Temporal → Conductor → Dapr → Restate:

| File | Engine | `--engine` value |
|---|---|---|
| `.demo/90-perf-cadence.yaml`   | Cadence   | `cadence`   |
| `.demo/91-perf-temporal.yaml`  | Temporal  | `temporal`  |
| `.demo/92-perf-conductor.yaml` | Conductor | `conductor` |
| `.demo/93-perf-dapr.yaml`      | Dapr      | `dapr`      |
| `.demo/94-perf-restate.yaml`   | Restate   | `restate`   |

## Decisions

- **Lifecycle:** Full lifecycle per file (start stack → start worker → run
  bench → show results → teardown). Mirrors the existing per-engine demo files
  (`02-cadence.yaml`, etc.).
- **Workflow count:** Fixed `--n 100` baked into the bench command. No
  `--reset` (results accumulate / overwrite the engine's row in place).
- **Base URL:** No `--base-url` override. The driver
  (`demos/perf/src/driver.ts`) already has correct per-engine defaults:
  cadence/temporal `:8080`, conductor `:5050`, dapr `:5467`, restate `:3000`.
- **Stack launch location:** Every stack command runs from the engine's own
  folder (`cd demos/0X-<engine> && <cmd>`) so all five files use an identical
  approach.
- **Results display:** `action: open` on `slides/04-performance/04-results.md`
  (plain markdown, no slide frontmatter — shown in the editor, not via
  `openSlide`). The driver rewrites this engine's row at the end of the run.
- **No title/section slides.** These files are operational (run the test), not
  presentation slides. The narrative slides already live in
  `.demo/10-performance.yaml`.

## Common scene skeleton

Each file uses `version: 3` and the following scenes (ids prefixed per engine,
e.g. `cadence-perf-start-stack`):

1. **`<engine>-perf-start-stack`** — `executeTerminalCommand`
   (`insertTypingMode: instant`, `autoExecute: true`) to start the stack, then
   `waitForInput` with a readiness message.
2. **`<engine>-perf-start-worker`** — `executeTerminalCommand` to start the
   app/worker, then `waitForInput` for readiness. **Omitted for Dapr** — `aspire
   run` starts the worker as part of the stack.
3. **`<engine>-perf-run-bench`** — `executeTerminalCommand`
   (`insertTypingMode: character-by-character`, `insertTypingSpeed: ~90`,
   `autoExecute: true`) running:
   `cd demos/perf && npm run bench -- --engine <name> --n 100`, then
   `waitForInput` so the speaker lets the run complete.
4. **`<engine>-perf-show-results`** — `action: open` on
   `slides/04-performance/04-results.md`, then `waitForInput` to discuss the
   numbers.
5. **`<engine>-perf-teardown`** — `focusTerminal` + `sendKeybinding ctrl+c` on
   the worker terminal, then on the stack terminal; `docker compose down -v`
   where the stack is Docker-based; `waitForTimeout`/`waitForInput` for
   shutdown; `closeTerminal` for each terminal used.

Setup commands use `insertTypingMode: instant` + `autoExecute: true` (audience
doesn't need to watch them typed). The bench command is typed
`character-by-character` so it's visible.

## Per-engine specifics

All stack commands run from the engine's own folder. Terminal IDs are reused
across the start/teardown of the same process.

| Engine | Stack cmd / `terminalId` | Worker cmd / `terminalId` | Teardown |
|---|---|---|---|
| Cadence   | `cd demos/01-cadence && docker compose up` / `cadence-cluster` | `cd demos/01-cadence && ./mvnw spring-boot:run` / `cadence-app` | ctrl+c both; `docker compose down -v` on `cadence-cluster` |
| Temporal  | `cd demos/02-temporal && temporal server start-dev` / `temporal-server` | `cd demos/02-temporal && ./mvnw spring-boot:run` / `temporal-app` | ctrl+c both |
| Conductor | `cd demos/03-conductor && docker compose up` / `conductor-server` | `cd demos/03-conductor && dotnet run` / `conductor-app` | ctrl+c both; `docker compose down -v` on `conductor-server` |
| Dapr      | `cd demos/04-dapr && aspire run` / `dapr-aspire` | *(none — Aspire runs the worker)* | ctrl+c `dapr-aspire` |
| Restate   | `cd demos/05-restate && restate-server` / `restate-server` | `cd demos/05-restate && npm run dev` / `restate-app` | ctrl+c both |

Readiness messages reuse the wording already proven in the matching
`0X-<engine>.yaml` files (e.g. Cadence: "Wait for Cassandra healthy + Cadence
server ready"; Temporal: "Wait for 'Server started' + Web UI URL"; etc.).

## Notes / caveats (carried into the YAML as speaker notes where relevant)

- **`testing=true`:** The perf driver sends `DiagnosticsInput.testing = true`,
  which skips the 60s engineering-review wait so the workflow body runs as fast
  as the engine allows. Each file's `description` states this.
- **Restate data directory:** `restate-server` writes its `restate-data/` to
  the current working directory. Running it from `demos/05-restate` creates a
  fresh data dir there, distinct from the repo-root `restate-data/` used by
  `06-restate.yaml`. Acceptable for a clean perf run.
- **Restate advertised address:** On a multi-homed host, `restate-server` can
  hang on "Trying to join cluster" unless launched with a loopback
  `RESTATE_ADVERTISED_ADDRESS`. This spec matches the existing `06-restate.yaml`
  (plain `restate-server`); a speaker note flags the workaround if it hangs.
- **Conductor / Restate runtimes:** Per `demos/perf/README.md`, Conductor and
  Restate currently show high wallclock/errors at the local single-node burst —
  expected; not something these files need to fix.

## Validation checklist (applied when authoring each file)

- Every `action:` matches a documented DemoTime action.
- Required params present and well-typed.
- All `path:` targets exist (`slides/04-performance/04-results.md`, the engine
  demo folders).
- Scene `id`s unique within each file.
- `terminalId` consistent between the start command and the matching
  `ctrl+c`/`closeTerminal`.

## Out of scope

- Modifying the perf driver (`demos/perf`) or the results markdown.
- Coordinating multiple engines in one run (driver is one-engine-per-invocation).
- Narrative performance slides (already in `.demo/10-performance.yaml`).
