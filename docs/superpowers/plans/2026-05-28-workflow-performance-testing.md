# Workflow Performance Testing — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a benchmark driver under `demos/perf/` and a `testing` skip-wait flag to every demo so each engine can be measured in isolation; populate a side-by-side results table in `slides/04-performance/04-results.md` one engine at a time.

**Architecture:** Five demos each gain a boolean `testing` field on `DiagnosticsInput` that, when `true`, skips the 60-second engineering-review wait. A standalone Node/TypeScript driver under `demos/perf/` fires N workflows at a single engine via its existing `/start` endpoint, polls `/status/{id}` for completion, computes wallclock/throughput/p50/p95/p99, writes per-run CSV under `demos/perf/results/`, and merges the engine's row into `slides/04-performance/04-results.md`.

**Tech Stack:** TypeScript 5 + Node 22 (tsx) for the driver; existing demo stacks unchanged (Java 21 + Maven, .NET 10, Node).

## ⚠️ Git policy for this plan

**Do not run `git add`, `git commit`, `git push`, `git checkout`, or any other git-mutating command.** Marc commits manually. Every place where a normal plan would say "Commit", this plan instead says:

> **Pause for user review.** Do not run any git command. Tell Marc what changed in this task and stop. He will stage, review, and commit himself.

Pre-existing read-only commands (`git status`, `git diff`, `git log`) are still fine when explicitly needed for context.

---

## File structure

**New files:**

```
demos/perf/
  package.json
  tsconfig.json
  README.md
  src/
    driver.ts             # CLI entrypoint + run flow
    stats.ts              # percentile + aggregate math (pure)
    csv.ts                # CSV writer (pure)
    results-md.ts         # markdown table merge/reset (pure)
    engines/
      types.ts            # EngineAdapter interface
      cadence.ts
      temporal.ts
      conductor.ts
      dapr.ts
      restate.ts
    __tests__/
      stats.test.ts
      csv.test.ts
      results-md.test.ts
  results/
    .gitkeep
slides/04-performance/
  04-results.md           # initial stub, all rows pending
```

**Modified files (per-demo `testing` flag):**

```
demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsInput.java
demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java
demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsInput.java
demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java
demos/03-conductor/Models/DiagnosticsInput.cs
demos/03-conductor/Workflows/DiagnosticsWorkflow.cs
demos/04-dapr/EnterpriseDiagnostics.ApiService/Models/DiagnosticsModels.cs
demos/04-dapr/EnterpriseDiagnostics.ApiService/Workflows/DiagnosticsWorkflow.cs
demos/05-restate/src/models.ts
demos/05-restate/src/diagnostics.ts
```

Driver code is split by responsibility (CLI vs pure math vs IO vs adapters) so each file stays small and individually testable. Engine-specific quirks live in `engines/<name>.ts`; the rest of the driver is engine-agnostic.

---

## Phase 1 — Add `testing` flag to each demo

### Task 1: Cadence — add `testing` flag and gate the wait

**Files:**
- Modify: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsInput.java`
- Modify: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java`

- [ ] **Step 1: Add `testing` field to `DiagnosticsInput`**

Replace the file with:

```java
package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsInput(
    String id,
    String shipName,
    String diagnosticsDate,
    String engineerName,
    boolean testing) {

    public DiagnosticsInput(String id, String shipName, String diagnosticsDate, String engineerName) {
        this(id, shipName, diagnosticsDate, engineerName, false);
    }
}
```

The convenience constructor preserves backwards compatibility with existing test fixtures that pass four arguments.

- [ ] **Step 2: Gate the 60s sleep on `testing`**

In `DiagnosticsWorkflowImpl.java`, replace lines around the existing sleep:

```java
        logger.info("Engineering review pending — sleeping 60s");
        Workflow.sleep(Duration.ofSeconds(60));
```

with:

```java
        if (!input.testing()) {
            logger.info("Engineering review pending — sleeping 60s");
            Workflow.sleep(Duration.ofSeconds(60));
        } else {
            logger.info("testing=true — skipping engineering review wait");
        }
```

- [ ] **Step 3: Verify build still passes**

Run: `cd demos/01-cadence && ./mvnw -q -DskipTests=false test`
Expected: BUILD SUCCESS; all existing tests pass because the convenience constructor keeps the four-arg form working.

- [ ] **Step 4: Manual smoke test — testing=true completes fast**

Start Cadence stack: `cd demos/01-cadence && docker compose up -d`
In a second terminal: `cd demos/01-cadence && ./mvnw spring-boot:run`
Once the app is up, run:

```bash
time curl -s -X POST http://localhost:8080/start \
  -H 'content-type: application/json' \
  -d '{"id":"perf-smoke-1","shipName":"Enterprise","diagnosticsDate":"2026-05-28","engineerName":"Marc","testing":true}'

# wait ~1-2s
curl -s http://localhost:8080/status/perf-smoke-1
```

Expected: `status` becomes `COMPLETED` in under ~3 seconds (vs ~62s with `testing:false`). If still 60s, the gate is not wired correctly.

Stop the app + compose stack when done.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc:

> Cadence demo updated: `DiagnosticsInput` has a new `testing` boolean (defaults to false via convenience ctor); the 60s wait in `DiagnosticsWorkflowImpl` is now gated on `!input.testing()`. Smoke test passed.

---

### Task 2: Temporal — add `testing` flag and gate the wait

**Files:**
- Modify: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsInput.java`
- Modify: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java`

- [ ] **Step 1: Add `testing` field to `DiagnosticsInput`**

Replace the file with:

```java
package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsInput(
    String id,
    String shipName,
    String diagnosticsDate,
    String engineerName,
    boolean testing) {

    public DiagnosticsInput(String id, String shipName, String diagnosticsDate, String engineerName) {
        this(id, shipName, diagnosticsDate, engineerName, false);
    }
}
```

- [ ] **Step 2: Gate the 60s sleep**

In `DiagnosticsWorkflowImpl.java`, replace the existing sleep block:

```java
        // KILL POINT — durable timer
        logger.info("Engineering review pending — sleeping 60s");
        Workflow.sleep(Duration.ofSeconds(60));
```

with:

```java
        if (!input.testing()) {
            // KILL POINT — durable timer
            logger.info("Engineering review pending — sleeping 60s");
            Workflow.sleep(Duration.ofSeconds(60));
        } else {
            logger.info("testing=true — skipping engineering review wait");
        }
```

- [ ] **Step 3: Verify build still passes**

Run: `cd demos/02-temporal && ./mvnw -q test`
Expected: BUILD SUCCESS.

- [ ] **Step 4: Manual smoke test**

Same as Task 1 Step 4 but for Temporal:

```bash
cd demos/02-temporal && docker compose up -d
cd demos/02-temporal && ./mvnw spring-boot:run    # second terminal
```

Then:

```bash
time curl -s -X POST http://localhost:8080/start \
  -H 'content-type: application/json' \
  -d '{"id":"perf-smoke-2","shipName":"Enterprise","diagnosticsDate":"2026-05-28","engineerName":"Marc","testing":true}'
sleep 2
curl -s http://localhost:8080/status/perf-smoke-2
```

Expected: COMPLETED within ~3 seconds.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc what changed and stop.

---

### Task 3: Conductor — add `Testing` flag and wrap the WaitTask in a SwitchTask

Conductor's wait is declarative (a `WaitTask`), so skipping requires a `SwitchTask` keyed on the `testing` input.

**Files:**
- Modify: `demos/03-conductor/Models/DiagnosticsInput.cs`
- Modify: `demos/03-conductor/Workflows/DiagnosticsWorkflow.cs`

- [ ] **Step 1: Add `Testing` to `DiagnosticsInput`**

Replace `Models/DiagnosticsInput.cs` with:

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record DiagnosticsInput(
    string Id,
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    bool Testing = false);
```

- [ ] **Step 2: Wrap the WaitTask in a SwitchTask gated on `testing`**

In `DiagnosticsWorkflow.cs`, locate the section that builds `engineeringReview` and adds it to `wf` (around lines 41 and 72). Replace:

```csharp
        var engineeringReview = new WaitTask("engineering_review", TimeSpan.FromSeconds(59));
```

with:

```csharp
        // SwitchTask gated on ${workflow.input.testing}. The "false" branch runs
        // the durable WaitTask; when testing=true no case matches and the switch
        // falls through immediately. Matches the in-code conditional used by the
        // other four engines.
        var engineeringReview = new WaitTask("engineering_review", TimeSpan.FromSeconds(59));
        var engineeringReviewSwitch = new SwitchTask("engineering_review_switch", "${workflow.input.testing}")
            .WithDecisionCase("false", BranchTasks(engineeringReview));
```

Then update the workflow assembly to use the switch instead of the bare WaitTask. In the `WithTask` chain (around line 72), replace:

```csharp
            .WithTask(engineeringReview)
```

with:

```csharp
            .WithTask(engineeringReviewSwitch)
```

Also update the input mapping to forward `testing`. The workflow input already arrives as JSON; the `${workflow.input.testing}` expression resolves to the string `"false"` or `"true"`. No additional `WithInputParameter` plumbing should be required, but verify in Step 4.

- [ ] **Step 3: Verify build still passes**

Run: `cd demos/03-conductor && dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Manual smoke test — confirm switch evaluation**

Start the Conductor stack: `cd demos/03-conductor && docker compose up -d`
Run: `cd demos/03-conductor && dotnet run` (this re-registers the workflow definition).

Once workers are connected:

```bash
time curl -s -X POST http://localhost:8080/start \
  -H 'content-type: application/json' \
  -d '{"id":"perf-smoke-3","shipName":"Enterprise","diagnosticsDate":"2026-05-28","engineerName":"Marc","testing":true}'
sleep 2
curl -s http://localhost:8080/status/perf-smoke-3
```

Expected: COMPLETED within ~3 seconds.

**If the workflow still waits the full 59 seconds with `testing:true`:**
The SwitchTask is comparing the bool against the string `"false"` literal incorrectly. Open the Conductor UI at `http://localhost:5000` (or the configured port), inspect the failed/waiting workflow, and look at the `engineering_review_switch` task's `evaluationResult`. Two likely fixes:
- Conductor may evaluate `${workflow.input.testing}` as JSON boolean — change the decision-case key from `"false"` to use the `WithDefaultCase(BranchTasks(engineeringReview))` form and add `WithDecisionCase("true", new WorkflowTask[0])` (empty branch). If empty branches are rejected by the SDK, add a `SetVariableTask` as a no-op placeholder.
- The input value may need a `WithInputParameter("testing", "${workflow.input.testing}")` on the switch task to expose it.

Pick whichever works and update the code comment in Step 2 to reflect the resolved form.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc which form ended up working (decision-case `"false"` vs default-case form) so the methodology slide can reference it.

---

### Task 4: Dapr — add `Testing` flag and gate the timer

**Files:**
- Modify: `demos/04-dapr/EnterpriseDiagnostics.ApiService/Models/DiagnosticsModels.cs`
- Modify: `demos/04-dapr/EnterpriseDiagnostics.ApiService/Workflows/DiagnosticsWorkflow.cs`

- [ ] **Step 1: Add `Testing` to `DiagnosticsInput`**

In `Models/DiagnosticsModels.cs`, replace the `DiagnosticsInput` record:

```csharp
public record DiagnosticsInput(
    string Id,
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    bool Testing = false);
```

- [ ] **Step 2: Gate the 60s timer**

In `Workflows/DiagnosticsWorkflow.cs`, replace lines 46-48:

```csharp
        // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
        LogSleeping(logger, context.InstanceId);
        await context.CreateTimer(TimeSpan.FromSeconds(60));
```

with:

```csharp
        // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
        if (!input.Testing)
        {
            LogSleeping(logger, context.InstanceId);
            await context.CreateTimer(TimeSpan.FromSeconds(60));
        }
        else
        {
            LogSleepSkipped(logger, context.InstanceId);
        }
```

Add the new logger message at the bottom of the class alongside the other `LoggerMessage` partials:

```csharp
    [LoggerMessage(LogLevel.Information, "Workflow {Id} testing=true — skipping engineering review wait")]
    static partial void LogSleepSkipped(ILogger logger, string Id);
```

- [ ] **Step 3: Verify build still passes**

Run: `cd demos/04-dapr && dotnet build`
Expected: Build succeeded.

- [ ] **Step 4: Manual smoke test**

Start the Aspire AppHost: `cd demos/04-dapr && dotnet run --project EnterpriseDiagnostics.AppHost`
Once running, find the ApiService port from Aspire dashboard, then:

```bash
time curl -s -X POST http://localhost:<port>/start \
  -H 'content-type: application/json' \
  -d '{"id":"perf-smoke-4","shipName":"Enterprise","diagnosticsDate":"2026-05-28","engineerName":"Marc","testing":true}'
sleep 2
curl -s http://localhost:<port>/status/perf-smoke-4
```

Expected: state shows COMPLETED within ~3 seconds.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc what changed and stop.

---

### Task 5: Restate — add `testing` flag and gate the sleep

**Files:**
- Modify: `demos/05-restate/src/models.ts`
- Modify: `demos/05-restate/src/diagnostics.ts`

- [ ] **Step 1: Add `testing` to `DiagnosticsInput`**

In `src/models.ts`, replace the `DiagnosticsInput` interface:

```ts
export interface DiagnosticsInput {
  id: string;
  shipName: string;
  diagnosticsDate: string;
  engineerName: string;
  testing?: boolean;
}
```

`testing` is optional so existing callers (HTTP files, demo scripts) keep working unchanged; absent ⇒ `false`.

- [ ] **Step 2: Gate the 60s sleep**

In `src/diagnostics.ts`, replace lines 41-44:

```ts
      ctx.console.log("All scans complete — engineering review pending, sleeping 60s");

      // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
      await ctx.sleep({ seconds: 60 });
```

with:

```ts
      if (!input.testing) {
        ctx.console.log("All scans complete — engineering review pending, sleeping 60s");
        // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
        await ctx.sleep({ seconds: 60 });
      } else {
        ctx.console.log("testing=true — skipping engineering review wait");
      }
```

- [ ] **Step 3: Verify build + tests pass**

Run: `cd demos/05-restate && npm test`
Expected: all existing tests pass (they don't exercise the workflow body directly so this is a smoke build).

Also: `cd demos/05-restate && npx tsc --noEmit`
Expected: no type errors.

- [ ] **Step 4: Manual smoke test**

Start Restate: `cd demos/05-restate && docker compose up -d` (or follow the project's README startup steps).
Start the service: `cd demos/05-restate && npm run dev`

Then:

```bash
time curl -s -X POST http://localhost:8080/DiagnosticsWorkflow/perf-smoke-5/run \
  -H 'content-type: application/json' \
  -d '{"id":"perf-smoke-5","shipName":"Enterprise","diagnosticsDate":"2026-05-28","engineerName":"Marc","testing":true}'
```

(Replace the URL with the exact Restate ingress endpoint from `enterprise-diagnostics.http` if it differs.)

Expected: response within ~3 seconds.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc what changed and stop.

---

## Phase 2 — Driver project scaffold

### Task 6: Create `demos/perf/` project skeleton

**Files:**
- Create: `demos/perf/package.json`
- Create: `demos/perf/tsconfig.json`
- Create: `demos/perf/results/.gitkeep`
- Create: `demos/perf/.gitignore`

- [ ] **Step 1: Verify the directory does not yet exist**

Run: `ls demos/perf 2>/dev/null || echo "ok, does not exist"`
Expected: `ok, does not exist`

- [ ] **Step 2: Create `demos/perf/package.json`**

```json
{
  "name": "durable-execution-battle-perf",
  "version": "0.1.0",
  "type": "module",
  "private": true,
  "scripts": {
    "bench": "tsx src/driver.ts",
    "test": "vitest run",
    "test:watch": "vitest",
    "typecheck": "tsc --noEmit"
  },
  "devDependencies": {
    "@types/node": "^22.0.0",
    "tsx": "^4.19.0",
    "typescript": "^5.6.0",
    "vitest": "^2.1.0"
  }
}
```

- [ ] **Step 3: Create `demos/perf/tsconfig.json`**

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "esModuleInterop": true,
    "strict": true,
    "skipLibCheck": true,
    "resolveJsonModule": true,
    "allowImportingTsExtensions": true,
    "noEmit": true,
    "types": ["node"]
  },
  "include": ["src/**/*"]
}
```

- [ ] **Step 4: Create `demos/perf/.gitignore` and results placeholder**

`demos/perf/.gitignore`:

```
node_modules/
results/*.csv
!results/.gitkeep
```

`demos/perf/results/.gitkeep`: empty file.

- [ ] **Step 5: Install dependencies and verify**

Run:
```bash
cd demos/perf && npm install
npm run typecheck
```

Expected: install succeeds (no `src/` yet, so typecheck has nothing to check — it should pass silently or warn that there are no input files; either is OK).

- [ ] **Step 6: Pause for user review**

Do not run any git command. Tell Marc the perf project skeleton is in place and stop.

---

### Task 7: Implement `stats.ts` (percentiles + aggregates) — TDD

**Files:**
- Create: `demos/perf/src/stats.ts`
- Create: `demos/perf/src/__tests__/stats.test.ts`

- [ ] **Step 1: Write the failing test**

`demos/perf/src/__tests__/stats.test.ts`:

```ts
import { describe, it, expect } from "vitest";
import { percentile, summarize } from "../stats.ts";

describe("percentile", () => {
  it("returns the p50 of an odd-length sorted-or-unsorted array", () => {
    expect(percentile([5, 1, 3, 2, 4], 50)).toBe(3);
  });

  it("interpolates between values for even-length arrays", () => {
    // [1,2,3,4] p50 → between 2 and 3 → 2.5
    expect(percentile([1, 2, 3, 4], 50)).toBe(2.5);
  });

  it("returns the maximum for p100", () => {
    expect(percentile([10, 20, 30], 100)).toBe(30);
  });

  it("returns the minimum for p0", () => {
    expect(percentile([10, 20, 30], 0)).toBe(10);
  });

  it("throws on empty input", () => {
    expect(() => percentile([], 50)).toThrow();
  });
});

describe("summarize", () => {
  it("computes p50/p95/p99, throughput, wallclock, errors from raw rows", () => {
    const rows = [
      { id: "a", t_start_ms: 1000, t_end_ms: 1100, latency_ms: 100, status: "COMPLETED" as const },
      { id: "b", t_start_ms: 1000, t_end_ms: 1300, latency_ms: 300, status: "COMPLETED" as const },
      { id: "c", t_start_ms: 1000, t_end_ms: 1500, latency_ms: 500, status: "COMPLETED" as const },
      { id: "d", t_start_ms: 1000, t_end_ms: 1900, latency_ms: 900, status: "TIMEOUT" as const },
    ];
    const s = summarize(rows, /*wallclock_ms*/ 900);
    expect(s.n).toBe(4);
    expect(s.completed).toBe(3);
    expect(s.errors).toBe(1);
    expect(s.wallclock_ms).toBe(900);
    expect(s.throughput_wps).toBeCloseTo(3 / 0.9, 3);
    // Percentiles are over completed rows only
    expect(s.p50_ms).toBe(300);
    expect(s.p95_ms).toBeGreaterThanOrEqual(500);
    expect(s.p99_ms).toBeGreaterThanOrEqual(500);
  });
});
```

- [ ] **Step 2: Run the test — verify it fails**

Run: `cd demos/perf && npm test`
Expected: FAIL — cannot resolve `../stats.ts`.

- [ ] **Step 3: Implement `stats.ts`**

`demos/perf/src/stats.ts`:

```ts
export type WorkflowRowStatus = "COMPLETED" | "TIMEOUT";

export interface WorkflowRow {
  id: string;
  t_start_ms: number;
  t_end_ms: number;
  latency_ms: number;
  status: WorkflowRowStatus;
}

export interface Summary {
  n: number;
  completed: number;
  errors: number;
  wallclock_ms: number;
  throughput_wps: number;
  p50_ms: number;
  p95_ms: number;
  p99_ms: number;
}

export function percentile(values: number[], p: number): number {
  if (values.length === 0) throw new Error("percentile of empty input");
  const sorted = [...values].sort((a, b) => a - b);
  if (p <= 0) return sorted[0];
  if (p >= 100) return sorted[sorted.length - 1];
  const rank = (p / 100) * (sorted.length - 1);
  const lo = Math.floor(rank);
  const hi = Math.ceil(rank);
  if (lo === hi) return sorted[lo];
  const frac = rank - lo;
  return sorted[lo] * (1 - frac) + sorted[hi] * frac;
}

export function summarize(rows: WorkflowRow[], wallclock_ms: number): Summary {
  const completed = rows.filter((r) => r.status === "COMPLETED");
  const errors = rows.length - completed.length;
  const latencies = completed.map((r) => r.latency_ms);
  const throughput_wps = wallclock_ms > 0 ? (completed.length / wallclock_ms) * 1000 : 0;
  return {
    n: rows.length,
    completed: completed.length,
    errors,
    wallclock_ms,
    throughput_wps,
    p50_ms: latencies.length ? percentile(latencies, 50) : 0,
    p95_ms: latencies.length ? percentile(latencies, 95) : 0,
    p99_ms: latencies.length ? percentile(latencies, 99) : 0,
  };
}
```

- [ ] **Step 4: Run the test — verify it passes**

Run: `cd demos/perf && npm test`
Expected: PASS — all `stats` tests green.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc `stats.ts` is implemented with passing tests and stop.

---

### Task 8: Implement `csv.ts` (CSV writer) — TDD

**Files:**
- Create: `demos/perf/src/csv.ts`
- Create: `demos/perf/src/__tests__/csv.test.ts`

- [ ] **Step 1: Write the failing test**

`demos/perf/src/__tests__/csv.test.ts`:

```ts
import { describe, it, expect } from "vitest";
import { rowsToCsv } from "../csv.ts";
import type { WorkflowRow } from "../stats.ts";

describe("rowsToCsv", () => {
  it("writes header + one line per row in stable column order", () => {
    const rows: WorkflowRow[] = [
      { id: "ship-0001", t_start_ms: 1, t_end_ms: 100, latency_ms: 99, status: "COMPLETED" },
      { id: "ship-0002", t_start_ms: 2, t_end_ms: 50,  latency_ms: 48, status: "TIMEOUT" },
    ];
    const csv = rowsToCsv(rows);
    expect(csv).toBe(
      "id,t_start_ms,t_end_ms,latency_ms,status\n" +
      "ship-0001,1,100,99,COMPLETED\n" +
      "ship-0002,2,50,48,TIMEOUT\n"
    );
  });

  it("returns header-only string for empty input", () => {
    expect(rowsToCsv([])).toBe("id,t_start_ms,t_end_ms,latency_ms,status\n");
  });
});
```

- [ ] **Step 2: Run the test — verify it fails**

Run: `cd demos/perf && npm test`
Expected: FAIL — cannot resolve `../csv.ts`.

- [ ] **Step 3: Implement `csv.ts`**

`demos/perf/src/csv.ts`:

```ts
import type { WorkflowRow } from "./stats.ts";

const HEADER = "id,t_start_ms,t_end_ms,latency_ms,status";

export function rowsToCsv(rows: WorkflowRow[]): string {
  const lines = rows.map(
    (r) => `${r.id},${r.t_start_ms},${r.t_end_ms},${r.latency_ms},${r.status}`,
  );
  return [HEADER, ...lines, ""].join("\n");
}
```

- [ ] **Step 4: Run the test — verify it passes**

Run: `cd demos/perf && npm test`
Expected: all `csv` and `stats` tests pass.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc CSV writer is implemented with passing tests.

---

### Task 9: Implement `results-md.ts` (table merge + reset) — TDD

This module owns the read/replace/write of the engine-row in `slides/04-performance/04-results.md`.

**Files:**
- Create: `demos/perf/src/results-md.ts`
- Create: `demos/perf/src/__tests__/results-md.test.ts`

- [ ] **Step 1: Write the failing test**

`demos/perf/src/__tests__/results-md.test.ts`:

```ts
import { describe, it, expect } from "vitest";
import { mergeRow, resetRow, ENGINES, initialResultsMarkdown } from "../results-md.ts";
import type { Summary } from "../stats.ts";

const STARTER = initialResultsMarkdown();

describe("initialResultsMarkdown", () => {
  it("contains a pending row for every engine", () => {
    for (const e of ENGINES) {
      expect(STARTER).toContain(`| ${e} `);
      expect(STARTER).toContain("_pending_");
    }
  });
});

describe("mergeRow", () => {
  const summary: Summary = {
    n: 100, completed: 100, errors: 0,
    wallclock_ms: 4200, throughput_wps: 23.8,
    p50_ms: 180, p95_ms: 410, p99_ms: 520,
  };

  it("replaces only the targeted engine's row, leaves others untouched", () => {
    const updated = mergeRow(STARTER, "cadence", summary, "2026-05-28 14:02");
    expect(updated).toContain("| cadence   | 100 | 4.2 s     | 23.8 wf/s  | 180 ms | 410 ms | 520 ms | 0      | 2026-05-28 14:02 |");
    // other engines still pending
    expect(updated).toContain("| temporal  | —   | _pending_");
    expect(updated).toContain("| restate   | —   | _pending_");
  });

  it("running twice for the same engine overwrites the previous row", () => {
    const once = mergeRow(STARTER, "dapr", summary, "2026-05-28 14:02");
    const twiceSummary: Summary = { ...summary, throughput_wps: 99.9, p50_ms: 50 };
    const twice = mergeRow(once, "dapr", twiceSummary, "2026-05-28 15:00");
    expect(twice).toContain("99.9 wf/s");
    expect(twice).toContain("50 ms");
    expect(twice).toContain("2026-05-28 15:00");
    expect(twice).not.toContain("23.8 wf/s");
  });
});

describe("resetRow", () => {
  it("sets the targeted engine's row back to pending without touching others", () => {
    const summary: Summary = {
      n: 100, completed: 100, errors: 0,
      wallclock_ms: 4200, throughput_wps: 23.8,
      p50_ms: 180, p95_ms: 410, p99_ms: 520,
    };
    const filled = mergeRow(STARTER, "conductor", summary, "2026-05-28 14:02");
    const reset = resetRow(filled, "conductor");
    expect(reset).toContain("| conductor | —   | _pending_");
    // Other rows from STARTER remain pending; reset didn't affect them
    expect(reset).toContain("| cadence   | —   | _pending_");
  });
});
```

- [ ] **Step 2: Run the test — verify it fails**

Run: `cd demos/perf && npm test`
Expected: FAIL — cannot resolve `../results-md.ts`.

- [ ] **Step 3: Implement `results-md.ts`**

`demos/perf/src/results-md.ts`:

```ts
import type { Summary } from "./stats.ts";

export const ENGINES = ["cadence", "temporal", "conductor", "dapr", "restate"] as const;
export type EngineName = (typeof ENGINES)[number];

const HEADER = [
  "# Performance — local burst (testing=true, wait skipped)",
  "",
  "| Engine    | N   | Wallclock | Throughput | p50    | p95    | p99    | Errors | Run at           |",
  "|-----------|-----|-----------|------------|--------|--------|--------|--------|------------------|",
];

const FOOTER = [
  "",
  "_Methodology: single-node local engines, default config; bursts of N workflows with `testing=true` (60s wait skipped); 200 ms status-poll floor; 5-workflow warmup discarded._",
  "",
];

function pendingRow(engine: EngineName): string {
  // Column widths chosen to match the header so the table stays aligned in source.
  const name = engine.padEnd(9, " ");
  return `| ${name} | —   | _pending_ |            |        |        |        |        |                  |`;
}

function filledRow(engine: EngineName, s: Summary, runAt: string): string {
  const name = engine.padEnd(9, " ");
  const n = String(s.n).padEnd(3, " ");
  const wall = `${(s.wallclock_ms / 1000).toFixed(1)} s`.padEnd(9, " ");
  const tput = `${s.throughput_wps.toFixed(1)} wf/s`.padEnd(10, " ");
  const p50 = `${Math.round(s.p50_ms)} ms`.padEnd(6, " ");
  const p95 = `${Math.round(s.p95_ms)} ms`.padEnd(6, " ");
  const p99 = `${Math.round(s.p99_ms)} ms`.padEnd(6, " ");
  const errs = String(s.errors).padEnd(6, " ");
  const at = runAt.padEnd(16, " ");
  return `| ${name} | ${n} | ${wall} | ${tput} | ${p50} | ${p95} | ${p99} | ${errs} | ${at} |`;
}

export function initialResultsMarkdown(): string {
  return [
    ...HEADER,
    ...ENGINES.map(pendingRow),
    ...FOOTER,
  ].join("\n");
}

function replaceEngineRow(md: string, engine: EngineName, newRow: string): string {
  const lines = md.split("\n");
  // Match a line starting with "| <engine>" possibly followed by spaces and a "|".
  const rowRegex = new RegExp(`^\\|\\s*${engine}\\b.*\\|\\s*$`);
  const idx = lines.findIndex((l) => rowRegex.test(l));
  if (idx === -1) {
    throw new Error(`results-md: no row found for engine "${engine}"`);
  }
  lines[idx] = newRow;
  return lines.join("\n");
}

export function mergeRow(md: string, engine: EngineName, s: Summary, runAt: string): string {
  return replaceEngineRow(md, engine, filledRow(engine, s, runAt));
}

export function resetRow(md: string, engine: EngineName): string {
  return replaceEngineRow(md, engine, pendingRow(engine));
}
```

- [ ] **Step 4: Run the test — verify it passes**

Run: `cd demos/perf && npm test`
Expected: PASS. If column-width mismatches break the literal-string assertion, adjust the padding constants in `filledRow`/`pendingRow` (and the test's literal string) so both agree. Alignment in source is nice-to-have; what matters is that other engines' rows are byte-for-byte preserved.

- [ ] **Step 5: Pause for user review**

Do not run any git command. Tell Marc results-md merge/reset is implemented with passing tests.

---

## Phase 3 — Engine adapters

### Task 10: Define the adapter interface and add all five adapters

**Files:**
- Create: `demos/perf/src/engines/types.ts`
- Create: `demos/perf/src/engines/cadence.ts`
- Create: `demos/perf/src/engines/temporal.ts`
- Create: `demos/perf/src/engines/conductor.ts`
- Create: `demos/perf/src/engines/dapr.ts`
- Create: `demos/perf/src/engines/restate.ts`

- [ ] **Step 1: Create the adapter type**

`demos/perf/src/engines/types.ts`:

```ts
export interface EngineAdapter {
  name: string;
  baseUrl: string;
  buildInput(id: string): unknown;
  // `start` accepts the JSON-serializable input; returns the workflow id the engine
  // assigned. For engines where the caller chooses the id, just return the same one.
  start(input: unknown): Promise<string>;
  // Polls the engine's status endpoint and returns true iff the workflow is done.
  isComplete(id: string): Promise<boolean>;
}

export interface AdapterFactory {
  create(baseUrl: string): EngineAdapter;
}
```

- [ ] **Step 2: Implement the Cadence adapter**

`demos/perf/src/engines/cadence.ts`:

```ts
import type { EngineAdapter } from "./types.ts";

export function createCadence(baseUrl: string): EngineAdapter {
  return {
    name: "cadence",
    baseUrl,
    buildInput(id: string) {
      return {
        id,
        shipName: "Enterprise",
        diagnosticsDate: "2026-05-28",
        engineerName: "perf-driver",
        testing: true,
      };
    },
    async start(input) {
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`cadence /start ${res.status}`);
      const body = (await res.json()) as { workflowId: string };
      return body.workflowId;
    },
    async isComplete(id) {
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (!res.ok) return false;
      const body = (await res.json()) as { status: string };
      return body.status === "COMPLETED";
    },
  };
}
```

- [ ] **Step 3: Implement the Temporal adapter**

`demos/perf/src/engines/temporal.ts`:

```ts
import type { EngineAdapter } from "./types.ts";

export function createTemporal(baseUrl: string): EngineAdapter {
  return {
    name: "temporal",
    baseUrl,
    buildInput(id: string) {
      return {
        id,
        shipName: "Enterprise",
        diagnosticsDate: "2026-05-28",
        engineerName: "perf-driver",
        testing: true,
      };
    },
    async start(input) {
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`temporal /start ${res.status}`);
      const body = (await res.json()) as { workflowId: string };
      return body.workflowId;
    },
    async isComplete(id) {
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (!res.ok) return false;
      const body = (await res.json()) as { status: string };
      return body.status === "COMPLETED";
    },
  };
}
```

- [ ] **Step 4: Implement the Conductor adapter**

`demos/perf/src/engines/conductor.ts`:

Inspect `demos/03-conductor/enterprise-diagnostics.http` to confirm the exact `/start` request shape and `/status` response shape (snake vs camel case, field names). The Conductor demo uses C# records with snake_case JSON via the Conductor SDK; verify before assuming.

```ts
import type { EngineAdapter } from "./types.ts";

export function createConductor(baseUrl: string): EngineAdapter {
  return {
    name: "conductor",
    baseUrl,
    buildInput(id: string) {
      // The Conductor C# record uses PascalCase property names; default ASP.NET
      // serialization produces camelCase JSON. Verify against the demo's .http
      // file and adjust field casing here if the actual API uses snake_case.
      return {
        id,
        shipName: "Enterprise",
        diagnosticsDate: "2026-05-28",
        engineerName: "perf-driver",
        testing: true,
      };
    },
    async start(input) {
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`conductor /start ${res.status}`);
      const body = (await res.json()) as { workflowId?: string; id?: string };
      const id = body.workflowId ?? body.id;
      if (!id) throw new Error("conductor /start: no workflowId in response");
      return id;
    },
    async isComplete(id) {
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (!res.ok) return false;
      const body = (await res.json()) as { status: string };
      return body.status === "COMPLETED";
    },
  };
}
```

- [ ] **Step 5: Implement the Dapr adapter**

`demos/perf/src/engines/dapr.ts`:

```ts
import type { EngineAdapter } from "./types.ts";

export function createDapr(baseUrl: string): EngineAdapter {
  return {
    name: "dapr",
    baseUrl,
    buildInput(id: string) {
      return {
        Id: id,
        ShipName: "Enterprise",
        DiagnosticsDate: "2026-05-28",
        EngineerName: "perf-driver",
        Testing: true,
      };
    },
    async start(input) {
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`dapr /start ${res.status}`);
      const body = (await res.json()) as { instanceId: string };
      return body.instanceId;
    },
    async isComplete(id) {
      // Dapr's /status/{id} returns 404 until the workflow exists; treat that as
      // "not yet complete" rather than an error.
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (res.status === 404) return false;
      if (!res.ok) return false;
      const body = (await res.json()) as {
        state?: { runtimeStatus?: string };
      };
      return body.state?.runtimeStatus === "COMPLETED";
    },
  };
}
```

Inspect the actual JSON shape via curl before relying on `runtimeStatus`; Dapr's `WorkflowState` may serialize it as `runtimeStatus` or as a `Completed` boolean depending on SDK version. Open `demos/04-dapr/EnterpriseDiagnostics.ApiService/Program.cs:42-51` and add a console.log in `isComplete` during development if needed.

- [ ] **Step 6: Implement the Restate adapter**

`demos/perf/src/engines/restate.ts`:

```ts
import type { EngineAdapter } from "./types.ts";

export function createRestate(baseUrl: string): EngineAdapter {
  return {
    name: "restate",
    baseUrl,
    buildInput(id: string) {
      return {
        id,
        shipName: "Enterprise",
        diagnosticsDate: "2026-05-28",
        engineerName: "perf-driver",
        testing: true,
      };
    },
    async start(input) {
      const wfId = (input as { id: string }).id;
      // Restate workflow invocation: POST /DiagnosticsWorkflow/{wfId}/run
      // Use the /send variant so the call returns immediately with the wf id.
      const res = await fetch(
        `${baseUrl}/DiagnosticsWorkflow/${encodeURIComponent(wfId)}/run/send`,
        {
          method: "POST",
          headers: { "content-type": "application/json" },
          body: JSON.stringify(input),
        },
      );
      if (!res.ok) throw new Error(`restate /run/send ${res.status}`);
      return wfId;
    },
    async isComplete(id) {
      // Workflow output endpoint returns 200 when complete, 470 (or similar) while running.
      // Validate the actual semantics against the running Restate ingress before relying on this.
      const res = await fetch(
        `${baseUrl}/restate/workflow/DiagnosticsWorkflow/${encodeURIComponent(id)}/output`,
      );
      return res.ok;
    },
  };
}
```

Inspect `demos/05-restate/enterprise-diagnostics.http` to confirm the exact ingress URL pattern; the example above follows the standard Restate ingress convention but the demo may have its own routing.

- [ ] **Step 7: Verify typecheck**

Run: `cd demos/perf && npm run typecheck`
Expected: no errors.

- [ ] **Step 8: Pause for user review**

Do not run any git command. Tell Marc all five adapters compile and stop. Note any adapter where the URL/shape was inferred and needs live verification in Task 11.

---

## Phase 4 — Driver runtime

### Task 11: Implement `driver.ts` (CLI + warmup + measured run + polling + IO)

**Files:**
- Create: `demos/perf/src/driver.ts`

This task wires the pure modules together. There's no unit test for the orchestration itself — it's exercised by end-to-end runs against a live engine in Step 4.

- [ ] **Step 1: Implement the driver**

`demos/perf/src/driver.ts`:

```ts
import { writeFile, readFile, mkdir } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { rowsToCsv } from "./csv.ts";
import { summarize, type WorkflowRow } from "./stats.ts";
import {
  ENGINES, type EngineName,
  initialResultsMarkdown, mergeRow, resetRow,
} from "./results-md.ts";
import type { EngineAdapter } from "./engines/types.ts";
import { createCadence } from "./engines/cadence.ts";
import { createTemporal } from "./engines/temporal.ts";
import { createConductor } from "./engines/conductor.ts";
import { createDapr } from "./engines/dapr.ts";
import { createRestate } from "./engines/restate.ts";

interface Args {
  engine: EngineName;
  baseUrl: string;
  n: number;
  concurrency: number;
  warmup: number;
  reset: boolean;
  pollMs: number;
  timeoutS: number;
}

const DEFAULT_BASE_URLS: Record<EngineName, string> = {
  cadence:   "http://localhost:8080",
  temporal:  "http://localhost:8080",
  conductor: "http://localhost:8080",
  dapr:      "http://localhost:8080",   // Aspire-assigned; pass --base-url to override
  restate:   "http://localhost:8080",
};

function parseArgs(argv: string[]): Args {
  const get = (flag: string, dflt?: string): string | undefined => {
    const i = argv.indexOf(flag);
    if (i === -1) return dflt;
    return argv[i + 1];
  };
  const engine = get("--engine") as EngineName | undefined;
  if (!engine || !ENGINES.includes(engine)) {
    throw new Error(`--engine is required and must be one of: ${ENGINES.join(", ")}`);
  }
  return {
    engine,
    baseUrl: get("--base-url", DEFAULT_BASE_URLS[engine])!,
    n: Number(get("--n", "100")),
    concurrency: Number(get("--concurrency", get("--n", "100"))),
    warmup: Number(get("--warmup", "5")),
    reset: argv.includes("--reset"),
    pollMs: Number(get("--poll-ms", "200")),
    timeoutS: Number(get("--timeout-s", "120")),
  };
}

function createAdapter(engine: EngineName, baseUrl: string): EngineAdapter {
  switch (engine) {
    case "cadence":   return createCadence(baseUrl);
    case "temporal":  return createTemporal(baseUrl);
    case "conductor": return createConductor(baseUrl);
    case "dapr":      return createDapr(baseUrl);
    case "restate":   return createRestate(baseUrl);
  }
}

async function runWithConcurrencyLimit<T>(
  items: T[],
  limit: number,
  fn: (item: T, index: number) => Promise<void>,
): Promise<void> {
  let cursor = 0;
  const workers = Array.from({ length: Math.min(limit, items.length) }, async () => {
    while (true) {
      const i = cursor++;
      if (i >= items.length) return;
      await fn(items[i], i);
    }
  });
  await Promise.all(workers);
}

async function waitForCompletion(
  adapter: EngineAdapter,
  id: string,
  pollMs: number,
  timeoutS: number,
): Promise<"COMPLETED" | "TIMEOUT"> {
  const deadline = Date.now() + timeoutS * 1000;
  while (Date.now() < deadline) {
    try {
      if (await adapter.isComplete(id)) return "COMPLETED";
    } catch {
      // transient errors are absorbed; the poll loop will retry
    }
    await new Promise((r) => setTimeout(r, pollMs));
  }
  return "TIMEOUT";
}

function formatRunAt(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

async function ensureResultsMarkdown(resultsPath: string): Promise<string> {
  if (existsSync(resultsPath)) {
    return await readFile(resultsPath, "utf8");
  }
  const initial = initialResultsMarkdown();
  await mkdir(path.dirname(resultsPath), { recursive: true });
  await writeFile(resultsPath, initial, "utf8");
  return initial;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const adapter = createAdapter(args.engine, args.baseUrl);

  console.log(
    `[perf] engine=${args.engine} n=${args.n} warmup=${args.warmup} concurrency=${args.concurrency}` +
    ` baseUrl=${args.baseUrl}` + (args.reset ? " --reset" : ""),
  );

  // Repository-root-relative paths so the driver works regardless of cwd.
  const here = path.dirname(fileURLToPath(import.meta.url));
  const repoRoot = path.resolve(here, "../../..");          // demos/perf/src → repo root
  const resultsMdPath = path.join(repoRoot, "slides/04-performance/04-results.md");
  const resultsDir = path.join(repoRoot, "demos/perf/results");

  let md = await ensureResultsMarkdown(resultsMdPath);

  if (args.reset) {
    md = resetRow(md, args.engine);
    await writeFile(resultsMdPath, md, "utf8");
    console.log(`[perf] reset ${args.engine} row → pending`);
  }

  // --- Warmup ---
  if (args.warmup > 0) {
    console.log(`[perf] warmup: ${args.warmup} workflows`);
    const warmupIds = Array.from({ length: args.warmup }, (_, i) => `warmup-${Date.now()}-${i}`);
    await runWithConcurrencyLimit(warmupIds, args.warmup, async (id) => {
      const wfId = await adapter.start(adapter.buildInput(id));
      await waitForCompletion(adapter, wfId, args.pollMs, args.timeoutS);
    });
    console.log("[perf] warmup complete");
  }

  // --- Measured run ---
  const ids = Array.from({ length: args.n }, (_, i) => `perf-${Date.now()}-${String(i).padStart(5, "0")}`);
  const rows: WorkflowRow[] = new Array(args.n);

  const wallStart = Date.now();
  await runWithConcurrencyLimit(ids, args.concurrency, async (id, i) => {
    const tStart = Date.now();
    let status: WorkflowRow["status"] = "TIMEOUT";
    try {
      const wfId = await adapter.start(adapter.buildInput(id));
      status = await waitForCompletion(adapter, wfId, args.pollMs, args.timeoutS);
    } catch (e) {
      status = "TIMEOUT";
      console.error(`[perf] ${id} failed:`, (e as Error).message);
    }
    const tEnd = Date.now();
    rows[i] = {
      id,
      t_start_ms: tStart,
      t_end_ms: tEnd,
      latency_ms: tEnd - tStart,
      status,
    };
  });
  const wallEnd = Date.now();

  const summary = summarize(rows, wallEnd - wallStart);
  console.log(
    `[perf] done: n=${summary.n} completed=${summary.completed} errors=${summary.errors}` +
    ` wallclock=${(summary.wallclock_ms / 1000).toFixed(2)}s` +
    ` throughput=${summary.throughput_wps.toFixed(2)}wf/s` +
    ` p50=${Math.round(summary.p50_ms)}ms p95=${Math.round(summary.p95_ms)}ms p99=${Math.round(summary.p99_ms)}ms`,
  );

  // --- Write CSV ---
  await mkdir(resultsDir, { recursive: true });
  const csvName = `${args.engine}-${args.n}-${new Date().toISOString().replace(/[:.]/g, "-")}.csv`;
  const csvPath = path.join(resultsDir, csvName);
  await writeFile(csvPath, rowsToCsv(rows), "utf8");
  console.log(`[perf] csv → ${csvPath}`);

  // --- Merge into results.md ---
  const updated = mergeRow(md, args.engine, summary, formatRunAt(new Date()));
  await writeFile(resultsMdPath, updated, "utf8");
  console.log(`[perf] updated ${resultsMdPath}`);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
```

- [ ] **Step 2: Typecheck**

Run: `cd demos/perf && npm run typecheck`
Expected: no errors.

- [ ] **Step 3: Run unit tests one more time**

Run: `cd demos/perf && npm test`
Expected: all stats / csv / results-md tests still pass.

- [ ] **Step 4: End-to-end test against one engine (pick whichever stack is easiest to start)**

Pick one engine — e.g. Cadence — and start it:

```bash
cd demos/01-cadence && docker compose up -d
cd demos/01-cadence && ./mvnw spring-boot:run    # second terminal
```

Once `/start` is reachable, in a third terminal:

```bash
cd demos/perf && npm run bench -- --engine cadence --n 10 --warmup 2
```

Expected behavior:
- Logs `[perf] engine=cadence n=10 ...`
- Logs `[perf] warmup: 2 workflows` then `[perf] warmup complete`
- Logs `[perf] done: n=10 completed=10 errors=0 wallclock=Xs throughput=Y wf/s p50=...`
- Writes `demos/perf/results/cadence-10-<timestamp>.csv`
- Writes/updates `slides/04-performance/04-results.md` with a cadence row populated and the other four rows still `_pending_`

If any of the inferred adapter fields (start response shape, status field) are wrong, fix the adapter in `demos/perf/src/engines/<engine>.ts` and re-run.

- [ ] **Step 5: Verify `--reset` works**

```bash
cd demos/perf && npm run bench -- --engine cadence --n 5 --warmup 1 --reset
```

Expected: the prior cadence row is reset to pending before the run, then refilled with new numbers at the end. Open `slides/04-performance/04-results.md` and confirm the other four rows are untouched throughout.

- [ ] **Step 6: Pause for user review**

Do not run any git command. Tell Marc the driver is end-to-end functional against at least one engine, point him at the generated CSV path and the updated results table, and note any adapter that still needs live validation (Conductor/Dapr/Restate shapes are inferred).

---

## Phase 5 — Initial results stub + README

### Task 12: Generate the initial `04-results.md` stub and write the perf README

**Files:**
- Create: `slides/04-performance/04-results.md` (will be created by the driver on first run, but generate explicitly here so the file exists in source control)
- Create: `demos/perf/README.md`

- [ ] **Step 1: Add a tiny scaffold script and run it**

Create `demos/perf/src/scaffold-results-md.ts`:

```ts
import { writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { initialResultsMarkdown } from "./results-md.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const out = path.resolve(here, "../../../slides/04-performance/04-results.md");
await mkdir(path.dirname(out), { recursive: true });
await writeFile(out, initialResultsMarkdown(), "utf8");
console.log("wrote", out);
```

Run it:

```bash
cd demos/perf && npx tsx src/scaffold-results-md.ts
```

Expected: `wrote /Users/marcduiker/.../slides/04-performance/04-results.md`. The file now exists with all five rows marked `_pending_`. The scaffold script can stay in the repo — it's also useful for re-initializing the table from scratch later.

- [ ] **Step 2: Inspect the generated file**

Run: `cat slides/04-performance/04-results.md`
Expected: a markdown table with header, five `_pending_` rows (one per engine), and the methodology footer.

- [ ] **Step 3: Write `demos/perf/README.md`**

```markdown
# Performance driver

Runs N copies of the shared `DiagnosticsWorkflow` against **one** workflow
engine at a time and writes the results into
`slides/04-performance/04-results.md`. Each demo's `DiagnosticsInput` has a
`testing` flag that, when `true`, skips the 60s engineering-review wait so the
workflow body runs as fast as the engine allows.

## Run one engine

Start the engine's stack first (see the engine's own README under
`demos/0X-<engine>/`). Then:

```bash
cd demos/perf
npm install            # first time only
npm run bench -- --engine cadence --n 100
```

Replace `cadence` with `temporal | conductor | dapr | restate`. One engine
per invocation — the driver does not coordinate multiple stacks.

## Flags

| Flag | Default | Meaning |
|---|---|---|
| `--engine` | *(required)* | `cadence` / `temporal` / `conductor` / `dapr` / `restate` |
| `--n` | 100 | Number of measured workflows |
| `--concurrency` | `--n` | Max in-flight `/start` calls (defaults to full burst) |
| `--warmup` | 5 | Throwaway workflows run before the measured set; not recorded |
| `--reset` | off | Reset this engine's row in `04-results.md` to `_pending_` before the run |
| `--poll-ms` | 200 | Status-poll interval. Latency floor. |
| `--timeout-s` | 120 | Per-workflow timeout |
| `--base-url` | `http://localhost:8080` | Override the engine's base URL |

## Outputs

- Per-run CSV: `demos/perf/results/<engine>-<N>-<ISO>.csv` (one row per workflow)
- Aggregate table: `slides/04-performance/04-results.md` (this engine's row replaced; others untouched)

## Methodology caveats

These numbers are for a conference-talk comparison, not a production benchmark.

- All engines run as single-node local stacks on default config.
- 200 ms status-poll interval is a latency floor; p50 < ~250 ms is meaningless.
- Engines are benchmarked on separate driver invocations; the table records a
  timestamp per row.
- Conductor's wait skip uses a `SwitchTask` wrapping the `WaitTask`; the other
  four engines use an in-code conditional. The Conductor workflow therefore has
  one extra structural node in both branches.
- Activities are mock work — these numbers reflect orchestration overhead
  primarily, not application logic.
```

(Note: in the fenced code block inside the README, the inner ` ```bash ` block needs to be escaped or the README's own fence broken. Replace with indented or single-line form if your markdown viewer struggles. The above renders fine in GitHub.)

- [ ] **Step 4: Pause for user review**

Do not run any git command. Tell Marc:

> Implementation complete. The full plan is now in place:
> - All five demos have a `testing` skip-wait flag (defaulting to off).
> - `demos/perf/` is a working Node driver with passing unit tests.
> - `slides/04-performance/04-results.md` exists with all five rows pending.
> - `demos/perf/README.md` documents the run flow.
>
> To populate the table, run the driver against each engine one at a time. Adapter shapes for Conductor / Dapr / Restate were inferred from the existing code and may need a small adjustment on first live run.

---

## Verification checklist

After all tasks complete, verify the following are true:

- [ ] `cd demos/01-cadence && ./mvnw -q test` passes
- [ ] `cd demos/02-temporal && ./mvnw -q test` passes
- [ ] `cd demos/03-conductor && dotnet test` passes (if there's a test project — there is one under `Tests/`)
- [ ] `cd demos/04-dapr && dotnet build` succeeds (no test project)
- [ ] `cd demos/05-restate && npm test && npx tsc --noEmit` passes
- [ ] `cd demos/perf && npm test && npm run typecheck` passes
- [ ] `slides/04-performance/04-results.md` exists, has 5 pending rows, methodology footer
- [ ] At least one end-to-end run against a live engine produced a populated row + CSV file
- [ ] Existing `enterprise-diagnostics.http` files still work without changes (default `testing=false`)
- [ ] Existing `.demo/*.yaml` scripts are unaffected
