# Perf report HTML Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **Git note:** This repo's owner commits manually. The "Checkpoint" step at the end of each task is where a commit would normally go — do NOT run `git commit`/`git add`. Just stop at the checkpoint and report status; the user commits.

**Goal:** Add `npm run report` to the perf project that reads `results/*.csv` and emits a single self-contained `report.html` with, per engine, an ECDF chart, a per-workflow sequence chart, and averaged p50/p95/p99.

**Architecture:** A pure `buildReportData()` function turns parsed CSV rows (grouped by engine) into a plain data object; a thin HTML assembler serializes that object to JSON, inlines the Plotly bundle, and writes `report.html`. CSV parsing is added to the existing `csv.ts`. Disk I/O lives only in the `main()` entry point so the data layer stays unit-testable.

**Tech Stack:** TypeScript (ESM, `.ts` import extensions), `tsx` runner, `vitest`, Plotly.js (`plotly.js-dist-min`, inlined for offline use).

**Spec:** `docs/superpowers/specs/2026-05-31-perf-report-html-design.md`

---

## File structure

- `demos/perf/src/csv.ts` — **modify**: add `parseCsv(text): WorkflowRow[]` next to the existing `rowsToCsv`.
- `demos/perf/src/__tests__/csv.test.ts` — **create**: tests for `parseCsv` (round-trips with `rowsToCsv`).
- `demos/perf/src/report.ts` — **create**: `buildReportData()` + types + HTML assembly + `main()`.
- `demos/perf/src/__tests__/report.test.ts` — **create**: tests for `buildReportData`.
- `demos/perf/package.json` — **modify**: add `plotly.js-dist-min` devDependency + `"report"` script.
- `demos/perf/report.html` — **generated output** (not hand-written).

---

## Task 1: CSV parser

**Files:**
- Modify: `demos/perf/src/csv.ts`
- Test: `demos/perf/src/__tests__/csv.test.ts` (create)

- [ ] **Step 1: Write the failing test**

Create `demos/perf/src/__tests__/csv.test.ts`:

```ts
import { describe, it, expect } from "vitest";
import { rowsToCsv, parseCsv } from "../csv.ts";
import type { WorkflowRow } from "../stats.ts";

describe("parseCsv", () => {
  it("parses a header + data rows into typed WorkflowRows", () => {
    const text =
      "id,t_start_ms,t_end_ms,latency_ms,status\n" +
      "a,1000,1100,100,COMPLETED\n" +
      "b,1000,1900,900,TIMEOUT\n";
    const rows = parseCsv(text);
    expect(rows).toEqual([
      { id: "a", t_start_ms: 1000, t_end_ms: 1100, latency_ms: 100, status: "COMPLETED" },
      { id: "b", t_start_ms: 1000, t_end_ms: 1900, latency_ms: 900, status: "TIMEOUT" },
    ]);
  });

  it("ignores a trailing blank line", () => {
    const text = "id,t_start_ms,t_end_ms,latency_ms,status\na,1,2,1,COMPLETED\n\n";
    expect(parseCsv(text)).toHaveLength(1);
  });

  it("round-trips with rowsToCsv", () => {
    const rows: WorkflowRow[] = [
      { id: "a", t_start_ms: 1000, t_end_ms: 1100, latency_ms: 100, status: "COMPLETED" },
      { id: "c", t_start_ms: 1000, t_end_ms: 1500, latency_ms: 500, status: "COMPLETED" },
    ];
    expect(parseCsv(rowsToCsv(rows))).toEqual(rows);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd demos/perf && npx vitest run src/__tests__/csv.test.ts`
Expected: FAIL — `parseCsv is not a function` (not exported).

- [ ] **Step 3: Write minimal implementation**

In `demos/perf/src/csv.ts`, add below `rowsToCsv` (the `HEADER` const and `WorkflowRow` import already exist):

```ts
import type { WorkflowRow, WorkflowRowStatus } from "./stats.ts";
```

(Change the existing import line to also bring in `WorkflowRowStatus`.) Then append:

```ts
export function parseCsv(text: string): WorkflowRow[] {
  const lines = text.split("\n").map((l) => l.trim()).filter((l) => l.length > 0);
  const body = lines[0] === HEADER ? lines.slice(1) : lines;
  return body.map((line) => {
    const [id, t_start_ms, t_end_ms, latency_ms, status] = line.split(",");
    return {
      id,
      t_start_ms: Number(t_start_ms),
      t_end_ms: Number(t_end_ms),
      latency_ms: Number(latency_ms),
      status: status as WorkflowRowStatus,
    };
  });
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd demos/perf && npx vitest run src/__tests__/csv.test.ts`
Expected: PASS (3 tests).

- [ ] **Step 5: Checkpoint** — report status, do not commit (see Git note).

---

## Task 2: Wire dependency and npm script

**Files:**
- Modify: `demos/perf/package.json`

- [ ] **Step 1: Add the Plotly dependency**

Run: `cd demos/perf && npm install --save-dev plotly.js-dist-min`
Expected: `plotly.js-dist-min` appears under `devDependencies` and `node_modules/plotly.js-dist-min/plotly.min.js` exists.

- [ ] **Step 2: Verify the bundle path exists**

Run: `cd demos/perf && test -f node_modules/plotly.js-dist-min/plotly.min.js && echo OK`
Expected: `OK`. (If the filename differs, run `ls node_modules/plotly.js-dist-min/` and use the actual `.min.js` path in Task 4.)

- [ ] **Step 3: Add the `report` script**

In `demos/perf/package.json`, add to `"scripts"` (after `"bench"`):

```json
    "report": "tsx src/report.ts",
```

- [ ] **Step 4: Checkpoint** — report status, do not commit.

---

## Task 3: `buildReportData` (pure data layer)

**Files:**
- Create: `demos/perf/src/report.ts`
- Test: `demos/perf/src/__tests__/report.test.ts` (create)

- [ ] **Step 1: Write the failing test**

Create `demos/perf/src/__tests__/report.test.ts`:

```ts
import { describe, it, expect } from "vitest";
import { buildReportData } from "../report.ts";
import type { WorkflowRow } from "../stats.ts";

function row(id: string, latency: number, status: "COMPLETED" | "TIMEOUT" = "COMPLETED"): WorkflowRow {
  return { id, t_start_ms: 0, t_end_ms: latency, latency_ms: latency, status };
}

describe("buildReportData", () => {
  it("groups files by engine prefix and orders engines by name", () => {
    const data = buildReportData([
      { name: "temporal-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
      { name: "cadence-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
    ]);
    expect(data.map((e) => e.engine)).toEqual(["cadence", "temporal"]);
  });

  it("labels runs Run 1..N in filename (timestamp) order", () => {
    const data = buildReportData([
      { name: "dapr-100-2026-05-29T18-00-00-000Z.csv", rows: [row("a", 100)] },
      { name: "dapr-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
    ]);
    const dapr = data.find((e) => e.engine === "dapr")!;
    expect(dapr.runs.map((r) => r.label)).toEqual(["Run 1", "Run 2"]);
    // Run 1 is the earliest timestamp
    expect(dapr.runs[0].source).toContain("T10-00-00");
  });

  it("computes per-run percentiles over COMPLETED rows only and counts errors", () => {
    const data = buildReportData([
      {
        name: "restate-100-2026-05-29T10-00-00-000Z.csv",
        rows: [row("a", 100), row("b", 300), row("c", 500), row("d", 900, "TIMEOUT")],
      },
    ]);
    const run = data[0].runs[0];
    expect(run.completed).toBe(3);
    expect(run.errors).toBe(1);
    expect(run.latencies).toEqual([100, 300, 500]);
    expect(run.p50).toBe(300);
  });

  it("averages each percentile across runs", () => {
    const data = buildReportData([
      { name: "conductor-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100), row("b", 100), row("c", 100)] },
      { name: "conductor-100-2026-05-29T11-00-00-000Z.csv", rows: [row("a", 300), row("b", 300), row("c", 300)] },
    ]);
    const c = data[0];
    // each run's p50 is its constant value → avg of 100 and 300 = 200
    expect(c.avg.p50).toBe(200);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd demos/perf && npx vitest run src/__tests__/report.test.ts`
Expected: FAIL — cannot find module `../report.ts` / `buildReportData` not exported.

- [ ] **Step 3: Write minimal implementation**

Create `demos/perf/src/report.ts` with the data layer only (HTML/main come in Task 4):

```ts
import { percentile, type WorkflowRow } from "./stats.ts";

export interface RunData {
  label: string;
  source: string;
  latencies: number[];
  completed: number;
  errors: number;
  p50: number;
  p95: number;
  p99: number;
}

export interface EngineReport {
  engine: string;
  runs: RunData[];
  avg: { p50: number; p95: number; p99: number };
}

export type ReportData = EngineReport[];

export interface CsvFile {
  name: string;
  rows: WorkflowRow[];
}

function engineOf(filename: string): string {
  return filename.split("-")[0];
}

function avg(nums: number[]): number {
  return nums.reduce((a, b) => a + b, 0) / nums.length;
}

export function buildReportData(files: CsvFile[]): ReportData {
  const byEngine = new Map<string, CsvFile[]>();
  for (const f of files) {
    const engine = engineOf(f.name);
    (byEngine.get(engine) ?? byEngine.set(engine, []).get(engine)!).push(f);
  }

  const engines = [...byEngine.keys()].sort();
  return engines.map((engine) => {
    const sorted = byEngine.get(engine)!.slice().sort((a, b) => a.name.localeCompare(b.name));
    const runs: RunData[] = sorted.map((f, i) => {
      const completedRows = f.rows.filter((r) => r.status === "COMPLETED");
      const latencies = completedRows.map((r) => r.latency_ms);
      return {
        label: `Run ${i + 1}`,
        source: f.name,
        latencies,
        completed: completedRows.length,
        errors: f.rows.length - completedRows.length,
        p50: latencies.length ? percentile(latencies, 50) : 0,
        p95: latencies.length ? percentile(latencies, 95) : 0,
        p99: latencies.length ? percentile(latencies, 99) : 0,
      };
    });
    return {
      engine,
      runs,
      avg: {
        p50: avg(runs.map((r) => r.p50)),
        p95: avg(runs.map((r) => r.p95)),
        p99: avg(runs.map((r) => r.p99)),
      },
    };
  });
}
```

Note: `stats.ts` exports `percentile` and the `WorkflowRow` type; importing the type with `import { ..., type WorkflowRow }` is valid here.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd demos/perf && npx vitest run src/__tests__/report.test.ts`
Expected: PASS (4 tests).

- [ ] **Step 5: Typecheck**

Run: `cd demos/perf && npm run typecheck`
Expected: no errors.

- [ ] **Step 6: Checkpoint** — report status, do not commit.

---

## Task 4: HTML assembly + entry point

**Files:**
- Modify: `demos/perf/src/report.ts` (append HTML assembly + `main`)

No new unit test — HTML string assembly is exercised end-to-end by running the script against the real CSVs (Step 4). Keep the assembler thin.

- [ ] **Step 1: Append HTML assembly and `main()` to `report.ts`**

Add these imports at the **top** of `demos/perf/src/report.ts` (alongside the existing import):

```ts
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { parseCsv } from "./csv.ts";
```

Then append at the **end** of the file:

```ts
function renderHtml(data: ReportData, plotlyJs: string): string {
  const sections = data
    .map(
      (e) => `
  <section class="engine">
    <h2>${e.engine}</h2>
    <table>
      <thead><tr><th>Run</th><th>p50 (ms)</th><th>p95 (ms)</th><th>p99 (ms)</th><th>completed</th><th>errors</th></tr></thead>
      <tbody>
        ${e.runs
          .map(
            (r) =>
              `<tr><td>${r.label}</td><td>${r.p50.toFixed(0)}</td><td>${r.p95.toFixed(0)}</td><td>${r.p99.toFixed(0)}</td><td>${r.completed}</td><td>${r.errors}</td></tr>`,
          )
          .join("\n        ")}
        <tr class="avg"><td>Average</td><td>${e.avg.p50.toFixed(0)}</td><td>${e.avg.p95.toFixed(0)}</td><td>${e.avg.p99.toFixed(0)}</td><td></td><td></td></tr>
      </tbody>
    </table>
    <div class="charts">
      <div id="ecdf-${e.engine}" class="chart"></div>
      <div id="seq-${e.engine}" class="chart"></div>
    </div>
  </section>`,
    )
    .join("\n");

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8" />
<title>Durable Execution Battle — performance</title>
<style>
  body { font-family: system-ui, sans-serif; margin: 2rem; color: #1a1a1a; }
  h1 { margin-bottom: 0.25rem; }
  .note { color: #555; max-width: 60rem; font-size: 0.9rem; }
  .engine { margin: 3rem 0; }
  table { border-collapse: collapse; margin: 1rem 0; }
  th, td { border: 1px solid #ccc; padding: 0.3rem 0.7rem; text-align: right; }
  th:first-child, td:first-child { text-align: left; }
  tr.avg { font-weight: 600; background: #f4f4f4; }
  .charts { display: flex; flex-wrap: wrap; gap: 1rem; }
  .chart { width: 560px; height: 360px; }
</style>
<script>${plotlyJs}</script>
</head>
<body>
<h1>Durable Execution Battle — performance</h1>
<p class="note">
  Conference-talk comparison, not a production benchmark. 200&nbsp;ms status-poll
  interval is a latency floor &mdash; p50 below ~250&nbsp;ms is meaningless. All
  engines run as single-node local stacks on default config; numbers reflect
  orchestration overhead, not application logic.
</p>
${sections}
<script>
  const REPORT = ${JSON.stringify(data)};
  const COLORS = ["#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd"];

  function ecdf(latencies) {
    const xs = [...latencies].sort((a, b) => a - b);
    const ys = xs.map((_, i) => ((i + 1) / xs.length) * 100);
    return { x: xs, y: ys };
  }

  for (const e of REPORT) {
    // ECDF chart
    const ecdfTraces = e.runs.map((r, i) => {
      const c = ecdf(r.latencies);
      return { x: c.x, y: c.y, mode: "lines", name: r.label, line: { color: COLORS[i % COLORS.length] } };
    });
    const pctLines = [
      { p: e.avg.p50, label: "avg p50" },
      { p: e.avg.p95, label: "avg p95" },
      { p: e.avg.p99, label: "avg p99" },
    ];
    Plotly.newPlot("ecdf-" + e.engine, ecdfTraces, {
      title: e.engine + " — latency ECDF",
      xaxis: { title: "latency (ms)" },
      yaxis: { title: "cumulative %", range: [0, 100] },
      shapes: pctLines.map((pl) => ({
        type: "line", x0: pl.p, x1: pl.p, y0: 0, y1: 100,
        line: { color: "#888", width: 1, dash: "dash" },
      })),
      annotations: pctLines.map((pl) => ({
        x: pl.p, y: 100, text: pl.label, showarrow: false, font: { size: 10 }, yanchor: "bottom",
      })),
      margin: { t: 40 },
    }, { responsive: true });

    // Per-workflow sequence chart
    const seqTraces = e.runs.map((r, i) => ({
      x: r.latencies.map((_, idx) => idx + 1),
      y: r.latencies,
      mode: "lines",
      name: r.label,
      line: { color: COLORS[i % COLORS.length] },
    }));
    Plotly.newPlot("seq-" + e.engine, seqTraces, {
      title: e.engine + " — duration per workflow",
      xaxis: { title: "workflow #" },
      yaxis: { title: "duration (ms)" },
      shapes: pctLines.map((pl) => ({
        type: "line", xref: "paper", x0: 0, x1: 1, y0: pl.p, y1: pl.p,
        line: { color: "#888", width: 1, dash: "dash" },
      })),
      annotations: pctLines.map((pl) => ({
        xref: "paper", x: 1, y: pl.p, text: pl.label, showarrow: false, font: { size: 10 }, xanchor: "right",
      })),
      margin: { t: 40 },
    }, { responsive: true });
  }
</script>
</body>
</html>`;
}

function main(): void {
  const here = dirname(fileURLToPath(import.meta.url));
  const resultsDir = join(here, "..", "results");
  const files: CsvFile[] = readdirSync(resultsDir)
    .filter((n) => n.endsWith(".csv"))
    .map((name) => ({ name, rows: parseCsv(readFileSync(join(resultsDir, name), "utf8")) }));

  if (files.length === 0) {
    console.error("No CSV files found in " + resultsDir);
    process.exit(1);
  }

  const data = buildReportData(files);
  const plotlyJs = readFileSync(
    join(here, "..", "node_modules", "plotly.js-dist-min", "plotly.min.js"),
    "utf8",
  );
  const html = renderHtml(data, plotlyJs);
  const out = join(here, "..", "report.html");
  writeFileSync(out, html);
  console.log("Wrote " + out + " (" + files.length + " CSV files, " + data.length + " engines)");
}

main();
```

- [ ] **Step 2: Typecheck**

Run: `cd demos/perf && npm run typecheck`
Expected: no errors.

- [ ] **Step 3: Run the full test suite**

Run: `cd demos/perf && npm test`
Expected: all tests pass (stats, csv, report, results-md).

- [ ] **Step 4: Generate the report against the real CSVs**

Run: `cd demos/perf && npm run report`
Expected: `Wrote .../demos/perf/report.html (15 CSV files, 5 engines)`.

- [ ] **Step 5: Verify the output opens and renders**

Run: `open demos/perf/report.html` (macOS).
Expected: a page with five engine sections (cadence, conductor, dapr, restate, temporal). Each has a summary table with an **Average** row, an ECDF chart with three run-lines + dashed p50/p95/p99 markers, and a per-workflow sequence chart with three lines. Charts render with **no internet connection** (Plotly is inlined). Confirm legend-click isolates a run and hover tooltips work.

- [ ] **Step 6: Checkpoint** — report status, do not commit.

---

## Self-review notes

- **Spec coverage:** ECDF + sequence chart per engine (Task 4); averaged p50/p95/p99 (Task 3 `avg`, rendered in table + marker lines); self-contained offline HTML with inlined Plotly (Task 4 `main`); reuse of `stats.ts` `percentile` (Task 3); all-files-per-engine, Run 1..N labelling (Task 3); methodology note (Task 4 HTML). Covered.
- **Missing-parser gap:** spec assumed an existing CSV parser; there is none, so Task 1 adds `parseCsv`. Covered.
- **Type consistency:** `RunData`/`EngineReport`/`ReportData`/`CsvFile` defined in Task 3 are the same names used by `renderHtml`/`main` in Task 4. `buildReportData` signature `(files: CsvFile[]) => ReportData` matches both the test and `main`.
