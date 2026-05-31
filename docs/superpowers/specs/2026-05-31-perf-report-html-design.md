# Perf report HTML — design

**Date:** 2026-05-31
**Status:** Approved (pending spec review)

## Goal

Visualize the durable-execution benchmark CSVs as a single, self-contained HTML
report for a conference talk. One section per workflow engine
(`cadence | conductor | dapr | restate | temporal`). Each engine has three data
sets (three benchmark runs of 100 workflows each). For each engine, show:

- **ECDF chart** — latency cumulative distribution, three runs overlaid.
- **Per-workflow sequence chart** — workflow index (x) vs duration in ms (y),
  three runs as lines.
- **p50 / p95 / p99** — averaged across the three runs (headline numbers).

## Decisions

| Decision | Choice |
|---|---|
| Build & delivery | TS build script → single self-contained `report.html` (data + charting lib inlined; opens offline by double-click, no server, no internet). |
| Charting library | Plotly.js (`plotly.js-dist-min`, inlined). |
| Sequence chart style | Lines (one per run), not bars. |
| Percentile method | Reuse `stats.ts` `percentile()` — Hyndman & Fan Type 6, identical to the slides table. |

## Components

### `demos/perf/src/report.ts` (new)

Entry point, run via `tsx`, wired as `"report"` in `package.json`. Two
responsibilities, kept separate:

1. **`buildReportData(files)`** — pure, testable. Returns a plain data object;
   no HTML, no I/O of the bundle.
2. **HTML assembly** — thin: serializes the data object to JSON, inlines the
   Plotly bundle, writes `demos/perf/report.html`.

### Dependency

Add `plotly.js-dist-min` as a devDependency. The build reads its bundle from
`node_modules` and inlines it into a `<script>` tag.

## Data pipeline (`buildReportData`)

1. Read every `results/*.csv`; parse with the existing `csv.ts` parser.
2. Group files by engine prefix (the substring before the first `-`):
   `cadence | conductor | dapr | restate | temporal`.
3. Sort each engine's files by the ISO timestamp in the filename; label them
   **Run 1..N** in that order. (Currently N = 3. If an engine ever has more
   than three CSVs, use **all** of them rather than picking the latest three.)
4. Per run: take `latency_ms` of `COMPLETED` rows. Compute p50/p95/p99 via
   `stats.ts` `percentile()`.
5. Per engine: **average** each percentile across the runs → the headline
   p50/p95/p99.

Output shape (illustrative):

```ts
interface RunData {
  label: string;          // "Run 1"
  source: string;         // csv filename
  latencies: number[];    // COMPLETED latency_ms, in workflow order
  completed: number;
  errors: number;
  p50: number; p95: number; p99: number;
}

interface EngineReport {
  engine: string;         // "cadence"
  runs: RunData[];
  avg: { p50: number; p95: number; p99: number };
}

type ReportData = EngineReport[];   // ordered by engine name
```

## HTML output (`demos/perf/report.html`)

Self-contained: Plotly bundle + `ReportData` JSON inlined. Opens by
double-click — no server, no CDN, no internet.

- **Top:** title + methodology note carried over from the README caveats
  (200 ms status-poll floor → p50 below ~250 ms is meaningless; single-node
  local stacks on default config; numbers reflect orchestration overhead, not
  application logic).
- **Per engine (5 sections):**
  - **Summary table** — rows Run 1/2/3 + **Average**; columns p50, p95, p99
    (ms), plus completed / errors per run.
  - **Chart A — ECDF.** x = latency (ms), y = cumulative %. Three run-lines
    overlaid. Vertical dashed reference lines at the **averaged** p50/p95/p99
    latencies, labelled.
  - **Chart B — sequence.** x = workflow index 1–100, y = duration (ms). Three
    run-lines. The same three averaged percentile values as horizontal dashed
    reference lines.
  - Plotly interactivity: hover tooltips, zoom, legend-click to isolate a run.

## Testing

`demos/perf/src/__tests__/report.test.ts` (vitest, existing setup) covers
`buildReportData`:

- Groups files by engine prefix correctly.
- Labels runs in timestamp order (Run 1 = earliest).
- Averages percentiles across runs.
- Counts completed vs errors; excludes non-COMPLETED rows from latency stats.

HTML string assembly is kept thin enough not to need its own test.

## Out of scope (YAGNI)

- No cross-engine combined comparison chart (one chart per engine only).
- No live re-reading of CSVs in the browser; data is baked at build time.
- No CLI flags for filtering engines/runs; build processes whatever is in
  `results/`.
