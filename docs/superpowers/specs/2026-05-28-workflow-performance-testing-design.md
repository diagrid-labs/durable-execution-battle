# Workflow Performance Testing — Design

**Status:** Draft
**Date:** 2026-05-28
**Author:** Marc Duiker

## Goal

Add a `Performance` section to the talk (`slides/04-performance/`) backed by reproducible local measurements across all five workflow demos (Cadence, Temporal, Conductor, Dapr, Restate). The same shared scenario is exercised in each engine and timing is collected from outside the workflow so the numbers are apples-to-apples.

Non-goal: a production-grade benchmark. This is a local, single-node, relative comparison for use in the talk.

## Scope

In scope:

- A `testing: bool` field on every demo's `DiagnosticsInput` that, when `true`, skips the 60-second engineering-review wait inside the workflow.
- A shared external driver under `demos/perf/` that fires N workflows at one engine, polls for completion, and writes results.
- A merged results table in `slides/04-performance/04-results.md` populated one engine at a time.

Out of scope:

- Activity-level instrumentation. Activities are mock work (string sampling, small random ops) and would mostly measure noise.
- Multi-engine concurrent benchmarks. One engine runs at a time; the driver targets a single engine per invocation.
- Sustained-rate (open-loop) load. Burst (closed-loop) of N=100 is enough for the talk.
- A scaling sweep (10 / 100 / 1000). Re-runnable later via `--n`.

## Per-demo change: `testing` skip-wait flag

Each demo's `DiagnosticsInput` gains a boolean `testing` field, defaulting to `false`. When `true`, the workflow body skips the wait step. Activities and the rest of the orchestration are unchanged.

| Demo | Input file | Wait construct today | Conditional form |
|---|---|---|---|
| 01-cadence | `model/DiagnosticsInput.java` | `Workflow.sleep(Duration.ofSeconds(60))` | `if (!input.testing()) Workflow.sleep(...);` |
| 02-temporal | `model/DiagnosticsInput.java` | `Workflow.sleep(Duration.ofSeconds(60))` | `if (!input.testing()) Workflow.sleep(...);` |
| 03-conductor | `Models/DiagnosticsInput.cs` | `new WaitTask("engineering_review", TimeSpan.FromSeconds(59))` | Wrap the `WaitTask` in a `SwitchTask` on `${workflow.input.testing}` so the wait branch is skipped when `testing=true`. See "Conductor structural risk" below. |
| 04-dapr | `Models/DiagnosticsModels.cs` (DiagnosticsInput record) | `await context.CreateTimer(TimeSpan.FromSeconds(60))` | `if (!input.Testing) await context.CreateTimer(...);` |
| 05-restate | `src/models.ts` | `await ctx.sleep({ seconds: 60 })` | `if (!input.testing) await ctx.sleep(...);` |

Default value is `false` everywhere so existing demo flows, `.demo` scripts, and `.http` files behave exactly as today without modification. Callers that want the fast path opt in explicitly.

### Conductor structural risk

Conductor's wait is a declarative `WaitTask`, not in-code. Skipping it requires wrapping it in a `SwitchTask` keyed on `${workflow.input.testing}`, which changes the workflow graph shape compared to the four code-driven engines. This is acknowledged as a methodological asymmetry — the benchmark still measures the same observable behavior (start → completion), but the Conductor workflow has one extra structural node in both branches. Document this in the methodology caveats slide.

## Driver: `demos/perf/`

A small Node + TypeScript project, matching the 05-restate stack to minimize new tooling.

### Layout

```
demos/perf/
  package.json
  tsconfig.json
  driver.ts
  engines/
    cadence.ts
    temporal.ts
    conductor.ts
    dapr.ts
    restate.ts
  results/
    <engine>-<N>-<timestamp>.csv
  README.md
```

Each `engines/<name>.ts` exports an adapter:

```ts
interface EngineAdapter {
  name: string;                           // "cadence"
  baseUrl: string;                        // http://localhost:<port>
  buildInput(id: string): unknown;        // shapes DiagnosticsInput for that engine
  startUrl: string;                       // "/start"
  statusUrl(id: string): string;          // "/status/{id}"
  isComplete(statusBody: unknown): boolean;
}
```

The driver code is engine-agnostic; only the adapter knows shape and URL details.

### CLI

```
npm run bench -- --engine <name> --n 100 [--concurrency N] [--warmup 5] [--reset] [--poll-ms 200] [--timeout-s 120]
```

Flags:

- `--engine` *(required)*: one of `cadence | temporal | conductor | dapr | restate`. Single engine per invocation.
- `--n`: number of workflows to fire in the measured run. Default 100.
- `--concurrency`: max in-flight starts. Default = `--n` (full burst).
- `--warmup`: number of throwaway workflows fired and awaited before the measured run, to amortize cold-start effects (JIT, connection pools, container warmup). Default 5. Warmup workflows are NOT included in the results CSV or aggregate table.
- `--reset`: clear this engine's row from `slides/04-performance/04-results.md` before running (sets it back to *pending*). Useful when re-running after a config change.
- `--poll-ms`: poll interval against `/status/{id}`. Default 200. This is the latency floor; the methodology caveats slide should note that p50 values below ~250 ms are not meaningful.
- `--timeout-s`: per-workflow timeout. If polling exceeds this without seeing COMPLETED, the workflow is recorded as an error. Default 120.

### Run flow

1. Load the engine adapter.
2. If `--reset`, rewrite `04-results.md` setting this engine's row to *pending* before continuing. `--reset` is not standalone; it always precedes a measured run in the same invocation.
3. Run warmup: fire `--warmup` workflows with `testing: true` and unique IDs, await all to COMPLETED, discard timings.
4. Measured run:
   a. Generate `--n` `DiagnosticsInput` payloads (`testing: true`, unique IDs).
   b. Capture `wallclock_start`.
   c. Fire `POST /start` for each, bounded by `--concurrency`. Record `t_start[i]` immediately before each POST.
   d. For each id, poll `/status/{id}` every `--poll-ms` until `isComplete()` is true (or `--timeout-s` elapses). Record `t_end[i]`.
   e. Capture `wallclock_end`.
5. Compute aggregates:
   - `wallclock_s = wallclock_end - wallclock_start`
   - `throughput_wps = n_completed / wallclock_s`
   - `p50_ms`, `p95_ms`, `p99_ms` over `t_end[i] - t_start[i]`
   - `errors = n - n_completed`
6. Write `results/<engine>-<N>-<timestamp>.csv` with one row per workflow (raw latencies).
7. Merge-on-write into `slides/04-performance/04-results.md`: parse the markdown, replace only this engine's row, rewrite the file. Rows for other engines are left untouched.

### CSV schema

```
id,t_start_ms,t_end_ms,latency_ms,status
ship-0001,1716894000123,1716894000412,289,COMPLETED
ship-0002,1716894000124,1716894000405,281,COMPLETED
...
```

`t_start_ms` and `t_end_ms` are Unix epoch milliseconds on the driver's clock. `status` is `COMPLETED` or `TIMEOUT` (timeouts contribute to the error count).

### Results table format

`slides/04-performance/04-results.md`:

```markdown
# Performance — local burst (N workflows, testing=true, wait skipped)

| Engine    | N   | Wallclock | Throughput | p50    | p95    | p99    | Errors | Run at           |
|-----------|-----|-----------|------------|--------|--------|--------|--------|------------------|
| cadence   | 100 | 4.2 s     | 23.8 wf/s  | 180 ms | 410 ms | 520 ms | 0      | 2026-05-28 14:02 |
| temporal  | —   | _pending_ |            |        |        |        |        |                  |
| conductor | —   | _pending_ |            |        |        |        |        |                  |
| dapr      | —   | _pending_ |            |        |        |        |        |                  |
| restate   | —   | _pending_ |            |        |        |        |        |                  |

_Methodology: single-node local engines, default config; bursts of N workflows with `testing=true` (60s wait skipped); 200 ms status-poll floor; 5-workflow warmup discarded._
```

The merge logic targets rows by the leading `| <engine>` cell. Other engines' rows are passed through unchanged.

## Methodology section in the talk

Alongside the numbers, the perf slides should call out:

- Single-node, default-config local engines — not a production benchmark.
- 200 ms poll floor — p50 below ~250 ms is not meaningful.
- Engines benchmarked on different runs, on the same machine. Timestamps per row.
- Conductor uses an extra `SwitchTask` to skip the wait; the other four use in-code conditionals.
- Activities are mock work — measured numbers reflect orchestration overhead, not application logic.

This is its own slide (`04-performance/05-methodology.md` or appended to `04-results.md`).

## File-by-file change list

New:

- `demos/perf/package.json`, `tsconfig.json`, `driver.ts`, `engines/{cadence,temporal,conductor,dapr,restate}.ts`, `README.md`
- `slides/04-performance/04-results.md` (initial stub with all rows *pending*)

Modified:

- `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsInput.java` — add `boolean testing` field (default false)
- `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java` — gate `Workflow.sleep`
- `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsInput.java` — same
- `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java` — gate `Workflow.sleep`
- `demos/03-conductor/Models/DiagnosticsInput.cs` — add `bool Testing` (default false)
- `demos/03-conductor/Workflows/DiagnosticsWorkflow.cs` — wrap `engineering_review` `WaitTask` in a `SwitchTask` on `${workflow.input.testing}`
- `demos/04-dapr/EnterpriseDiagnostics.ApiService/Models/DiagnosticsModels.cs` — add `bool Testing` to `DiagnosticsInput` record (default false)
- `demos/04-dapr/EnterpriseDiagnostics.ApiService/Workflows/DiagnosticsWorkflow.cs` — gate `CreateTimer`
- `demos/05-restate/src/models.ts` — add `testing?: boolean` to `DiagnosticsInput`
- `demos/05-restate/src/diagnostics.ts` — gate `ctx.sleep`

Unmodified by design:

- All activity / worker / task files
- All existing `enterprise-diagnostics.http` files (they don't set `testing`, default is `false`, behavior unchanged)
- All `.demo/*.yaml` scripts
- Existing `/start` and `/status/{id}` endpoint signatures

## Risks and open questions

1. **Conductor structural asymmetry** (covered above). Accepted with a methodology caveat.
2. **Driver clock vs engine clock.** `t_start` is captured just before the POST returns; `t_end` is the first poll observation of COMPLETED. Real end can be up to `--poll-ms` earlier. This biases all engines equally and the methodology caveat covers it.
3. **Restate workflow IDs are workflow keys.** The driver must generate distinct ids per workflow run; collisions become "already running" errors. Same constraint applies to all engines and matches existing `/start` semantics.
4. **`--reset` interaction with concurrent driver runs.** Out of scope — single user, single machine.

## Acceptance criteria

- All five demos build and pass their existing tests with the new `testing` field defaulted to `false`.
- Running each demo via its existing `.http` file produces identical observable behavior to today (60s wait still happens).
- `npm run bench -- --engine <name> --n 100` against a running instance of that engine produces a CSV and updates that engine's row in `04-results.md`.
- The results table can be populated one engine at a time across separate driver invocations without losing previously written rows.
- `--reset --engine <name>` sets that engine's row back to *pending* and leaves the other four rows intact.
